using SalesService.Messaging.Events;

namespace SalesService.Messaging.Publisher
{
    public interface IRabbitMqPublisher : IAsyncDisposable
    {
        Task Publish(OrderCreatedEvent orderCreatedEvent);
        Task PublishAsync<T>(string queueName, T message);
    }
}
