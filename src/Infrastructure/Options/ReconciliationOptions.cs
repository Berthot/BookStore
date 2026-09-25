using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options;

/// <summary>Controls the cadence of the purchase reconciliation background job.</summary>
public sealed class ReconciliationOptions
{
    public const string SectionName = "Reconciliation";

    [Range(1, int.MaxValue)]
    public int IntervalSeconds { get; set; } = 300;

    [Range(1, int.MaxValue)]
    public int StalenessThresholdSeconds { get; set; } = 600;
}
