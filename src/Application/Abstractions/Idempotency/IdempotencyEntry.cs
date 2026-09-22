using Domain.Bases;

namespace Application.Abstractions.Idempotency;

/// <summary>Records an idempotency key and its associated response so repeated requests can replay the original result.</summary>
public sealed class IdempotencyEntry : Entity
{
    public const int KeyMaxLength = 255;
    public const int HashMaxLength = 64;

    public string Key { get; init; } = string.Empty;
    public string BodyHash { get; init; } = string.Empty;
    public string Status { get; private set; } = IdempotencyStatus.Processing;
    public string? ResponseBody { get; private set; }
    public Guid? ResourceId { get; private set; }

    public static IdempotencyEntry Create(string key, string bodyHash, DateTime now) =>
        new() { Id = Guid.NewGuid(), Key = key, BodyHash = bodyHash, CreatedAt = now };

    public void Complete(string responseBody, Guid? resourceId = null)
    {
        Status = IdempotencyStatus.Completed;
        ResponseBody = responseBody;
        ResourceId = resourceId;
        UpdatedAt = DateTime.UtcNow;
    }
}

public static class IdempotencyStatus
{
    public const string Processing = "Processing";
    public const string Completed = "Completed";
}
