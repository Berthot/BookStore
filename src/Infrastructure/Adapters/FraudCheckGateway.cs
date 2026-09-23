using Application.UseCases.FraudAnalysis.SubmitTransaction;
using Application.UseCases.Sales.Ports;
using Cortex.Mediator;
using Domain.Enums;

namespace Infrastructure.Adapters;

public sealed class FraudCheckGateway(IMediator mediator) : IFraudCheckGateway
{
    public async Task<Guid> SubmitAsync(
        Guid purchaseId,
        string bookFormat,
        int quantity,
        decimal totalAmount,
        string currency,
        string paymentType,
        string paymentFingerprint,
        string? paymentLast4,
        string customerId,
        string correlationId,
        DateTime placedAt,
        CancellationToken cancellationToken = default)
    {
        // EBOOK maps to Digital; all physical formats map to Physical (D-17).
        var delivery = bookFormat.Equals("EBOOK", StringComparison.OrdinalIgnoreCase)
            ? DeliveryType.Digital.ToString()
            : DeliveryType.Physical.ToString();

        // purchaseId is the external reference — serves as idempotency key on the fraud side (D-36/D-41).
        var result = await mediator.SendCommandAsync(
            new SubmitTransactionRequest(
                purchaseId.ToString(),
                customerId,
                totalAmount,
                currency,
                paymentType,
                paymentFingerprint,
                paymentLast4,
                Channel.Api.ToString(),
                delivery,
                quantity,
                placedAt,
                correlationId),
            cancellationToken);

        if (!result.IsSuccess || result.Data is null)
            throw new InvalidOperationException(
                $"SubmitTransaction failed [{result.ErrorCode}]: {string.Join("; ", result.Errors)}");

        return result.Data.TransactionId;
    }
}
