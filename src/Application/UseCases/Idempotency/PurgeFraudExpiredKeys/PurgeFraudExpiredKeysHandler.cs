using Application.Abstractions.Idempotency;
using Application.Commons;
using Cortex.Mediator.Commands;

namespace Application.UseCases.Idempotency.PurgeFraudExpiredKeys;

public sealed class PurgeFraudExpiredKeysHandler(IFraudIdempotencyStore fraudStore)
    : ICommandHandler<PurgeFraudExpiredKeysRequest, OperationResult<PurgeFraudExpiredKeysResponse>>
{
    public async Task<OperationResult<PurgeFraudExpiredKeysResponse>> Handle(
        PurgeFraudExpiredKeysRequest command,
        CancellationToken cancellationToken)
    {
        var deleted = await fraudStore.DeleteExpiredAsync(command.Before, cancellationToken);
        return OperationResult<PurgeFraudExpiredKeysResponse>.SuccessResult(
            new PurgeFraudExpiredKeysResponse(deleted));
    }
}
