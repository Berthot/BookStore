using OpenTelemetry.Trace;

namespace Infrastructure.Extensions;

/// <summary>Drops root DB spans that have no parent trace (outbox polling, background jobs, EF migrations).</summary>
internal sealed class RootDbSpanSuppressor(Sampler inner) : Sampler
{
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        if (samplingParameters.ParentContext.TraceId == default
            && samplingParameters.Tags is not null
            && HasDbSystemTag(samplingParameters.Tags))
            return new SamplingResult(SamplingDecision.Drop);

        return inner.ShouldSample(samplingParameters);
    }

    private static bool HasDbSystemTag(IEnumerable<KeyValuePair<string, object?>> tags)
    {
        foreach (var tag in tags)
            if (tag.Key == "db.system") return true;
        return false;
    }
}
