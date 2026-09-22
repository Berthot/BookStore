namespace Infrastructure.Persistence.Seed;

/// <summary>Seeds catalogue reference data. Idempotent — safe to call multiple times.</summary>
public interface ICatalogSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
