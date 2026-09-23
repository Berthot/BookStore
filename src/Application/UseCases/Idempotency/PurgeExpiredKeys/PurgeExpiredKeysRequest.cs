using Application.Commons;
using Cortex.Mediator.Commands;

namespace Application.UseCases.Idempotency.PurgeExpiredKeys;

public sealed record PurgeExpiredKeysRequest(DateTime Before)
    : ICommand<OperationResult<PurgeExpiredKeysResponse>>;
