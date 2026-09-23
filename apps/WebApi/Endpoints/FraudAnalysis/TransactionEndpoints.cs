using Application.Abstractions.Idempotency;
using Application.UseCases.FraudAnalysis.GetTransaction;
using Application.UseCases.FraudAnalysis.SubmitTransaction;
using Cortex.Mediator;
using Domain.Repositories;
using WebApi.Extensions;
using WebApi.Filters.Idempotency;
using WebApi.OpenApi;

namespace WebApi.Endpoints.FraudAnalysis;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/transactions").WithTags("Transactions");

        group.MapPost("/", SubmitAsync)
            .AddEndpointFilter<IdempotencyFilter<IFraudIdempotencyStore>>()
            .RequireIdempotencyKey()
            .WithName("SubmitTransaction");

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetTransaction");

        return app;
    }

    private static async Task<IResult> SubmitAsync(
        SubmitTransactionHttpRequest body,
        HttpContext httpContext,
        IFraudUnitOfWork unitOfWork,
        IFraudIdempotencyStore idempotencyStore,
        IMediator mediator,
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

        // Complete the idempotency entry and commit it
        if (httpContext.Items[IdempotencyFilter<IFraudIdempotencyStore>.HttpContextEntryKey] is IdempotencyEntry entry)
        {
            var responseJson = System.Text.Json.JsonSerializer.Serialize(result.Data);
            entry.Complete(responseJson, result.Data!.TransactionId);
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

// HTTP request DTOs (not commands — they carry raw JSON fields before mapping)
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
