using Application.Commons;
using Cortex.Mediator.Queries;
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

        // Only surface fraud details once the decision is final.
        // Detailed score/rules are available via GET /api/v1/transactions/{id} in Fraud.Api.
        FraudDetailsResponse? fraudDetails = purchase.Status switch
        {
            PurchaseStatus.Confirmed => new FraudDetailsResponse(purchase.TransactionId, Outcome.Approved, null, null),
            PurchaseStatus.Cancelled => new FraudDetailsResponse(purchase.TransactionId, Outcome.Rejected, null, null),
            _ => null
        };

        return OperationResult<GetPurchaseResponse>.SuccessResult(new GetPurchaseResponse(
            purchase.Id,
            purchase.Status,
            GetCustomerMessage(purchase.Status),
            fraudDetails,
            purchase.CreatedAt));
    }

    private static string GetCustomerMessage(PurchaseStatus status) => status switch
    {
        PurchaseStatus.PendingFraudCheck => InAnalysisMessage,
        PurchaseStatus.UnderReview => InAnalysisMessage,
        PurchaseStatus.Confirmed => ConfirmedMessage,
        PurchaseStatus.Cancelled => CancelledMessage,
        _ => InAnalysisMessage
    };
}
