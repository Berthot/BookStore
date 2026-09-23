namespace Application.Abstractions.Messaging;

/// <summary>Publishes application events to the bus. Infrastructure routes them through the transactional outbox so messages are never lost on commit failure.</summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}
