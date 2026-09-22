namespace Tests.Shared.Constants;

public static class TestConstants
{
    public static class Ids
    {
        public static readonly Guid BookId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public static readonly Guid PurchaseId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        public static readonly Guid TransactionId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        public static readonly Guid CustomerId = Guid.Parse("00000000-0000-0000-0000-000000000004");
    }

    public static class Dates
    {
        public static readonly DateTime FixedUtcNow = new(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);
    }
}
