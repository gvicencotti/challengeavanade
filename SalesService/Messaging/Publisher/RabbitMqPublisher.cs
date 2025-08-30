using RabbitMQ.Client;
using SalesService.Messaging.Events;
using System.Text;
using System.Text.Json;

namespace SalesService.Messaging.Publisher
{
    public class RabbitMqPublisher : IRabbitMqPublisher
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        public RabbitMqPublisher()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "rabbitmq"
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.QueueDeclareAsync(
                queue: "order-created",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            ).GetAwaiter().GetResult();

            Console.WriteLine("[Publisher] Conexão com RabbitMQ estabelecida e fila 'order-created' declarada.");
        }

        public async Task PublishAsync<T>(string queueName, T message)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            Console.WriteLine($"[Publisher] Publicando na fila '{queueName}': {json}");

            await _channel.BasicPublishAsync<BasicProperties>(
                exchange: "",
                routingKey: queueName,
                mandatory: false,
                basicProperties: new BasicProperties { DeliveryMode = DeliveryModes.Persistent },
                body: body
            );

            Console.WriteLine($"[Publisher] Evento {typeof(T).Name} publicado com sucesso na fila '{queueName}'.");
        }

        public Task Publish(OrderCreatedEvent orderCreatedEvent)
        {
            Console.WriteLine("[Publisher] Chamado Publish(OrderCreatedEvent).");
            return PublishAsync("order-created", orderCreatedEvent);
        }

        public async ValueTask DisposeAsync()
        {
            Console.WriteLine("[Publisher] Encerrando conexão com RabbitMQ...");
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
        }
    }
}
