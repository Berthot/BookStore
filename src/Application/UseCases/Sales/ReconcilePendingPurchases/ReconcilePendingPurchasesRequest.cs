using Application.Commons;
using Cortex.Mediator.Commands;

namespace Application.UseCases.Sales.ReconcilePendingPurchases;

public sealed record ReconcilePendingPurchasesRequest(DateTime Threshold)
    : ICommand<OperationResult<ReconcilePendingPurchasesResponse>>;
