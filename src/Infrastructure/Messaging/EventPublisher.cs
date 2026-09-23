using Application.Abstractions.Messaging;
using MassTransit;

namespace Infrastructure.Messaging;

internal sealed class EventPublisher(IPublishEndpoint endpoint) : IEventPublisher
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class =>
        endpoint.Publish(message, cancellationToken);
}
