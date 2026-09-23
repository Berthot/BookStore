using Application.Commons;
using Cortex.Mediator.Queries;
using Domain.Entities.Sales;
using Domain.Enums;
using Domain.Repositories;

namespace Application.UseCases.Sales.GetPurchase;

public sealed class GetPurchaseHandler(
    IPurchaseRepository repository,
    ITransactionRepository transactionRepository)
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

        var fraudDetails = await GetFraudDetailsAsync(purchase, cancellationToken);

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

    private async Task<FraudDetailsResponse?> GetFraudDetailsAsync(Purchase p, CancellationToken ct)
    {
        if (!p.TransactionId.HasValue)
        {
            // No transaction linked yet; infer outcome from final status for Confirmed/Cancelled
            // purchases that somehow lost the transaction link (defensive fallback, D-40).
            if (p.Status is PurchaseStatus.Confirmed)
                return new FraudDetailsResponse(null, Outcome.Approved, null, null);
            if (p.Status is PurchaseStatus.Cancelled)
                return new FraudDetailsResponse(null, Outcome.Rejected, null, null);
            return null;
        }

        var transaction = await transactionRepository.GetByIdAsync(p.TransactionId.Value, ct);
        var assessment = transaction?.CurrentAssessment();

        var outcome = assessment?.Outcome
            ?? p.Status switch
            {
                PurchaseStatus.Confirmed => (Outcome?)Outcome.Approved,
                PurchaseStatus.Cancelled => Outcome.Rejected,
                _ => null
            };

        var score = assessment is not null
            ? (int?)((int)Math.Round(assessment.Evaluations.Sum(e => e.Weight) * 100))
            : null;

        var triggered = assessment?.Evaluations
            .Where(e => e.Hit)
            .Select(e => new TriggeredRuleSummary(e.RuleCode, e.Reason))
            .ToList();

        return new FraudDetailsResponse(p.TransactionId, outcome, score, triggered);
    }
}
