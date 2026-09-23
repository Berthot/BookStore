using Application.Abstractions.Idempotency;
using Application.UseCases.Sales.GetPurchase;
using Application.UseCases.Sales.PurchaseBook;
using Cortex.Mediator;
using Domain.Repositories;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WebApi.Extensions;
using WebApi.Filters.Idempotency;
using WebApi.OpenApi;

namespace WebApi.Endpoints.Sales;

public static class PurchaseEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/purchases").WithTags("Purchases");

        group.MapPost("/", PurchaseAsync)
            .AddEndpointFilter<IdempotencyFilter<IBookStoreIdempotencyStore>>()
            .RequireIdempotencyKey()
            .WithName("PurchaseBook");

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetPurchase");

        return api;
    }

    private static async Task<IResult> PurchaseAsync(
        PurchaseHttpRequest body,
        HttpContext httpContext,
        IBookStoreUnitOfWork unitOfWork,
        IBookStoreIdempotencyStore idempotencyStore,
        IMediator mediator,
        IOptions<JsonOptions> jsonOptions,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();

        var command = new PurchaseBookRequest(
            body.BookId,
            body.Quantity,
            body.Payment.Type,
            body.Payment.Fingerprint,
            body.Payment.Last4,
            body.CustomerId,
            correlationId);

        var result = await mediator.SendCommandAsync(command, cancellationToken);

        if (!result.IsSuccess)
            return result.ToHttpResult();

        if (httpContext.Items[IdempotencyFilter<IBookStoreIdempotencyStore>.HttpContextEntryKey] is IdempotencyEntry entry)
        {
            var responseJson = JsonSerializer.Serialize(result.Data, jsonOptions.Value.SerializerOptions);
            entry.Complete(responseJson, result.Data!.PurchaseId);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return Results.Accepted($"/api/v1/purchases/{result.Data!.PurchaseId}", result.Data);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.SendQueryAsync(new GetPurchaseRequest(id), cancellationToken);
        return result.ToHttpResult();
    }
}

public sealed record PurchaseHttpRequest(Guid BookId, int Quantity, string CustomerId, PurchasePaymentDto Payment);

public sealed record PurchasePaymentDto(string Type, string Fingerprint, string? Last4);
