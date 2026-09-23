using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

/// <summary>
/// Wraps every consumer invocation in an ILogger scope that carries CorrelationId, MessageType and
/// MessageId, making all consumer logs filterable by those fields in structured log back-ends.
/// </summary>
public sealed class LoggingScopeConsumeFilter<T>(ILogger<LoggingScopeConsumeFilter<T>> logger)
    : IFilter<ConsumeContext<T>>
    where T : class
{
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = context.CorrelationId?.ToString(),
            ["MessageType"] = typeof(T).Name,
            ["MessageId"] = context.MessageId?.ToString()
        }))
        {
            await next.Send(context);
        }
    }

    public void Probe(ProbeContext context) { }
}
