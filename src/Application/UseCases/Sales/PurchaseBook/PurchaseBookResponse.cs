using Domain.Enums;

namespace Application.UseCases.Sales.PurchaseBook;

public sealed record PurchaseBookResponse(Guid PurchaseId, PurchaseStatus Status);
