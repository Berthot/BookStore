using Application.Commons;
using Cortex.Mediator.Commands;

namespace Application.UseCases.Idempotency.PurgeFraudExpiredKeys;

public sealed record PurgeFraudExpiredKeysRequest(DateTime Before)
    : ICommand<OperationResult<PurgeFraudExpiredKeysResponse>>;
