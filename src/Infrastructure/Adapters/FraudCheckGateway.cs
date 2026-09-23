using Application.UseCases.FraudAnalysis.SubmitTransaction;
using Application.UseCases.Sales.Ports;
using Cortex.Mediator;

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
        var delivery = bookFormat.Equals("EBOOK", StringComparison.OrdinalIgnoreCase) ? "Digital" : "Physical";

        var result = await mediator.SendCommandAsync(
            new SubmitTransactionRequest(
                purchaseId.ToString(),
                customerId,
                totalAmount,
                currency,
                paymentType,
                paymentFingerprint,
                paymentLast4,
                "Api",
                delivery,
                quantity,
                placedAt,
                correlationId),
            cancellationToken);

        return result.Data!.TransactionId;
    }
}
