using Application.Abstractions.Idempotency;
using Application.UseCases.FraudAnalysis.GetTransaction;
using Application.UseCases.FraudAnalysis.SubmitTransaction;
using Cortex.Mediator;
using Domain.Repositories;
using Fraud.Api.Infrastructure.Extensions;
using Fraud.Api.Infrastructure.Filters.Idempotency;
using Fraud.Api.Infrastructure.OpenApi;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Fraud.Api.Endpoints.FraudAnalysis;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/transactions").WithTags("Transactions");

        group.MapPost("/", SubmitAsync)
            .Produces<SubmitTransactionResponse>(202)
            .ProducesProblem(400)
            .ProducesProblem(409)
            .ProducesProblem(422)
            .AddEndpointFilter<IdempotencyFilter<IFraudIdempotencyStore>>()
            .RequireIdempotencyKey()
            .WithTransactionExamples()
            .WithName("SubmitTransaction");

        group.MapGet("/{id:guid}", GetAsync)
            .Produces<GetTransactionResponse>(200)
            .ProducesProblem(404)
            .WithName("GetTransaction");

        return api;
    }

    private static async Task<IResult> SubmitAsync(
        SubmitTransactionHttpRequest body,
        HttpContext httpContext,
        IFraudUnitOfWork unitOfWork,
        IFraudIdempotencyStore idempotencyStore,
        IMediator mediator,
        IOptions<JsonOptions> jsonOptions,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();

        var command = new SubmitTransactionRequest(
            body.ExternalReference,
            body.CustomerId,
            body.Amount.Value,
            body.Amount.Currency,
            body.Payment.Type,
            body.Payment.Fingerprint,
            body.Payment.Last4,
            body.Channel,
            body.Delivery,
            body.ItemCount,
            body.OccurredAt,
            correlationId);

        var result = await mediator.SendCommandAsync(command, cancellationToken);

        if (!result.IsSuccess)
            return result.ToHttpResult();

        if (httpContext.Items[IdempotencyFilter<IFraudIdempotencyStore>.HttpContextEntryKey] is IdempotencyEntry entry)
        {
            var responseJson = JsonSerializer.Serialize(result.Data, jsonOptions.Value.SerializerOptions);
            entry.Complete(responseJson, 202, $"/api/v1/transactions/{result.Data!.TransactionId}", result.Data!.TransactionId);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return Results.Accepted($"/api/v1/transactions/{result.Data!.TransactionId}", result.Data);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.SendQueryAsync(new GetTransactionRequest(id), cancellationToken);
        return result.ToHttpResult();
    }
}

public sealed record SubmitTransactionHttpRequest(
    string? ExternalReference,
    string CustomerId,
    AmountDto Amount,
    PaymentDto Payment,
    string Channel,
    string Delivery,
    int ItemCount,
    DateTime OccurredAt);

public sealed record AmountDto(decimal Value, string Currency);
public sealed record PaymentDto(string Type, string Fingerprint, string? Last4);
