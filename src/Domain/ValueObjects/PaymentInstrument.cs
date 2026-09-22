namespace Domain.ValueObjects;

/// <summary>Represents a payment instrument. Never stores the full card number — only Last4 and Fingerprint.</summary>
public sealed record PaymentInstrument(string Type, string Fingerprint, string? Last4 = null);
