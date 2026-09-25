namespace Application.UseCases.Idempotency.PurgeFraudExpiredKeys;

public sealed record PurgeFraudExpiredKeysResponse(int TotalDeleted);
