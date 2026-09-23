using Application.Commons;
using Cortex.Mediator.Queries;

namespace Application.UseCases.Sales.GetPurchase;

public sealed record GetPurchaseRequest(Guid PurchaseId) : IQuery<OperationResult<GetPurchaseResponse>>;
