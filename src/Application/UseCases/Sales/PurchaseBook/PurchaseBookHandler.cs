using Application.Abstractions.Messaging;
using Application.Commons;
using Application.Messages;
using Cortex.Mediator.Commands;
using Domain.Entities.Sales;
using Domain.Repositories;
using Domain.ValueObjects;

namespace Application.UseCases.Sales.PurchaseBook;

public sealed class PurchaseBookHandler(
    IBookRepository bookRepository,
    IPurchaseRepository purchaseRepository,
    IBookStoreUnitOfWork unitOfWork,
    IEventPublisher publisher)
    : ICommandHandler<PurchaseBookRequest, OperationResult<PurchaseBookResponse>>
{
    public async Task<OperationResult<PurchaseBookResponse>> Handle(
        PurchaseBookRequest command,
        CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(command.BookId, cancellationToken);
        if (book is null)
            return OperationResult<PurchaseBookResponse>.Fail(ErrorCode.NotFound, "Book not found.");

        var total = new Money(book.Price.Value * command.Quantity, book.Price.Currency);
        var purchase = Purchase.Create(command.BookId, command.Quantity, total, command.CorrelationId, DateTime.UtcNow);

        purchaseRepository.Add(purchase);

        await publisher.PublishAsync(new PurchasePlaced(
            purchase.Id,
            book.Id,
            purchase.Quantity,
            total.Value,
            total.Currency,
            book.Format.ToString().ToUpperInvariant(),
            command.PaymentType,
            command.PaymentFingerprint,
            command.PaymentLast4,
            command.CustomerId,
            command.CorrelationId,
            purchase.CreatedAt), cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        return OperationResult<PurchaseBookResponse>.SuccessResult(
            new PurchaseBookResponse(purchase.Id, purchase.Status.ToString().ToUpperInvariant()));
    }
}
