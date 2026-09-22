namespace Domain.ValueObjects;

public enum DeciderKind { Engine, Reviewer, System }

public sealed record Decider(DeciderKind Kind, string? ReviewerId = null);
