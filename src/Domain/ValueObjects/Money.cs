using Domain.Bases;

namespace Domain.ValueObjects;

public sealed record Money(decimal Value, string Currency)
{
    public static DomainError? Validate(decimal value, string currency)
    {
        if (value <= 0) return new DomainError("MONEY_INVALID_VALUE", "Money value must be greater than zero.");
        if (string.IsNullOrWhiteSpace(currency)) return new DomainError("MONEY_INVALID_CURRENCY", "Currency is required.");
        return null;
    }
}
