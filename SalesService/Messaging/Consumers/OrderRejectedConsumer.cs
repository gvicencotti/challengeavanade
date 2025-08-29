using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SalesService.Data;
using Shared.Messaging.Events;

namespace SalesService.Messaging.Consumers
{
    public class OrderRejectedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;

        public OrderRejectedConsumer(IServiceScopeFactory scopeFactory)
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
                queue: "order-rejected",
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

                    var orderRejected = JsonSerializer.Deserialize<OrderRejectedEvent>(message);

                    if (orderRejected != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<SalesContext>();

                        var order = await context.Orders
                            .FirstOrDefaultAsync(o => o.Id == orderRejected.OrderId, stoppingToken);

                        if (order != null)
                        {
                            order.Status = "Rejected";
                            await context.SaveChangesAsync(stoppingToken);

                            Console.WriteLine($"🚨 Pedido {order.Id} rejeitado: {orderRejected.Reason}");
                        }
                    }

                    if (_channel != null)
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar OrderRejectedEvent: {ex.Message}");

                    if (_channel != null)
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: "order-rejected",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken
            );

            Console.WriteLine("OrderRejectedConsumer iniciado e ouvindo mensagens...");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.CloseAsync(cancellationToken);
            if (_connection != null) await _connection.CloseAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}
