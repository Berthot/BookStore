namespace Application.UseCases.Idempotency.PurgeExpiredKeys;

public sealed record PurgeExpiredKeysResponse(int TotalDeleted);
