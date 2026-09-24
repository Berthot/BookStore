namespace Infrastructure.Options;

/// <summary>Controls database startup behaviour.</summary>
public sealed class DatabaseOptions
{
    public const string Section = "Database";

    /// <summary>When true, the WebApi applies pending EF Core migrations during startup.</summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
