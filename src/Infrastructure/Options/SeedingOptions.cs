namespace Infrastructure.Options;

/// <summary>Controls catalogue data seeding during application startup.</summary>
public sealed class SeedingOptions
{
    public const string Section = "Seeding";

    /// <summary>When true, CatalogDataSeeder runs after migrations (idempotent — safe to enable in production).</summary>
    public bool Enabled { get; set; }
}
