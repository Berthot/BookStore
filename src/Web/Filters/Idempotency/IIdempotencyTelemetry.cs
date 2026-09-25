namespace Web.Filters.Idempotency;

public interface IIdempotencyTelemetry
{
    void RecordRequest(string result);
}
