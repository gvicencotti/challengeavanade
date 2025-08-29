using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StockService.Data;
using Shared.Messaging.Events;

namespace StockService.Messaging.Consumers
{
    public class OrderCreatedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;

        public OrderCreatedConsumer(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

            _factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _connection = await _factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: "order-created",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken
            );

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    Console.WriteLine($"[Consumer] Mensagem recebida da fila 'order-created': {message}");

                    var orderCreated = JsonSerializer.Deserialize<OrderCreatedEvent>(message);

                    if (orderCreated != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<StockContext>();

                        bool estoqueInsuficiente = false;

                        foreach (var item in orderCreated.Items)
                        {
                            var product = await context.Products
                                .FirstOrDefaultAsync(p => p.Id == item.ProductId, stoppingToken);

                            if (product == null || product.Quantity < item.Quantity)
                            {
                                estoqueInsuficiente = true;

                                var rejectedEvent = new OrderRejectedEvent
                                {
                                    OrderId = orderCreated.OrderId,
                                    ProductId = item.ProductId,
                                    RequestedQuantity = item.Quantity,
                                    AvailableQuantity = product?.Quantity ?? 0,
                                    Reason = "Estoque insuficiente para o produto."
                                };

                                Console.WriteLine($"[Consumer] Publicando OrderRejectedEvent para Produto {item.ProductId}.");
                                await PublishEvent("order-rejected", rejectedEvent);
                            }
                        }

                        if (!estoqueInsuficiente)
                        {
                            foreach (var item in orderCreated.Items)
                            {
                                var product = await context.Products
                                    .FirstOrDefaultAsync(p => p.Id == item.ProductId, stoppingToken);

                                if (product != null)
                                {
                                    product.Quantity -= item.Quantity;

                                    var stockUpdatedEvent = new StockUpdatedEvent
                                    {
                                        OrderId = orderCreated.OrderId,
                                        ProductId = item.ProductId,
                                        QuantityReduced = item.Quantity,
                                        NewStock = product.Quantity
                                    };

                                    Console.WriteLine($"[Consumer] Publicando StockUpdatedEvent para Produto {item.ProductId}. Novo estoque: {product.Quantity}");
                                    await PublishEvent("stock-updated", stockUpdatedEvent);
                                }
                            }

                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }

                    if (_channel != null)
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Consumer] Erro ao processar mensagem: {ex.Message}");

                    if (_channel != null)
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: "order-created",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken
            );

            Console.WriteLine("OrderCreatedConsumer iniciado e ouvindo mensagens...");
        }

        private async Task PublishEvent<T>(string queueName, T @event)
        {
            if (_channel == null) return;

            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                mandatory: false,
                basicProperties: props,
                body: body
            );

            Console.WriteLine($"[Publisher] Evento {typeof(T).Name} publicado na fila '{queueName}'.");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.CloseAsync(cancellationToken);
            if (_connection != null) await _connection.CloseAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}
