using Application.Abstractions.Idempotency;
using Application.Commons;
using Cortex.Mediator.Commands;

namespace Application.UseCases.Idempotency.PurgeExpiredKeys;

public sealed class PurgeExpiredKeysHandler(
    IBookStoreIdempotencyStore bookStoreStore,
    IFraudIdempotencyStore fraudStore)
    : ICommandHandler<PurgeExpiredKeysRequest, OperationResult<PurgeExpiredKeysResponse>>
{
    public async Task<OperationResult<PurgeExpiredKeysResponse>> Handle(
        PurgeExpiredKeysRequest command,
        CancellationToken cancellationToken)
    {
        var bookStoreDeleted = await bookStoreStore.DeleteExpiredAsync(command.Before, cancellationToken);
        var fraudDeleted = await fraudStore.DeleteExpiredAsync(command.Before, cancellationToken);

        return OperationResult<PurgeExpiredKeysResponse>.SuccessResult(
            new PurgeExpiredKeysResponse(bookStoreDeleted + fraudDeleted));
    }
}
