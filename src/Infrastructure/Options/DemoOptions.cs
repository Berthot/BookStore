namespace Infrastructure.Options;

/// <summary>Demo/development options that slow down processing for visible async behaviour.</summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    public int FraudProcessingDelaySeconds { get; set; } = 0;
}
