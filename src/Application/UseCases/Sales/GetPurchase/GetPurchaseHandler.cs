using Application.Commons;
using Cortex.Mediator.Queries;
using Domain.Entities.Sales;
using Domain.Enums;
using Domain.Repositories;

namespace Application.UseCases.Sales.GetPurchase;

public sealed class GetPurchaseHandler(IPurchaseRepository repository)
    : IQueryHandler<GetPurchaseRequest, OperationResult<GetPurchaseResponse>>
{
    private const string InAnalysisMessage = "Pagamento em análise.";
    private const string ConfirmedMessage = "Pagamento confirmado.";
    private const string CancelledMessage = "Pagamento cancelado.";

    public async Task<OperationResult<GetPurchaseResponse>> Handle(
        GetPurchaseRequest query,
        CancellationToken cancellationToken)
    {
        var purchase = await repository.GetByIdAsync(query.PurchaseId, cancellationToken);
        if (purchase is null)
            return OperationResult<GetPurchaseResponse>.Fail(ErrorCode.NotFound, "Purchase not found.");

        return OperationResult<GetPurchaseResponse>.SuccessResult(MapToResponse(purchase));
    }

    internal static GetPurchaseResponse MapToResponse(Purchase p) =>
        new(
            p.Id,
            p.Status,
            GetCustomerMessage(p.Status),
            GetFraudDetails(p),
            p.CreatedAt);

    private static string GetCustomerMessage(PurchaseStatus status) => status switch
    {
        PurchaseStatus.PendingFraudCheck => InAnalysisMessage,
        PurchaseStatus.UnderReview => InAnalysisMessage,
        PurchaseStatus.Confirmed => ConfirmedMessage,
        PurchaseStatus.Cancelled => CancelledMessage,
        _ => InAnalysisMessage
    };

    private static FraudDetailsResponse? GetFraudDetails(Purchase p)
    {
        if (p.Status is PurchaseStatus.Confirmed)
            return new FraudDetailsResponse(p.TransactionId, "APPROVED");
        if (p.Status is PurchaseStatus.Cancelled)
            return new FraudDetailsResponse(p.TransactionId, "REJECTED");
        return p.TransactionId.HasValue
            ? new FraudDetailsResponse(p.TransactionId, null)
            : null;
    }
}
