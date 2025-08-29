using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SalesService.Data;
using Shared.Messaging.Events;

namespace SalesService.Messaging.Consumers
{
    public class StockUpdatedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;

        public StockUpdatedConsumer(IServiceScopeFactory scopeFactory)
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
                queue: "stock-updated",
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

                    var stockUpdated = JsonSerializer.Deserialize<StockUpdatedEvent>(message);

                    if (stockUpdated != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<SalesContext>();

                        var order = await context.Orders
                            .FirstOrDefaultAsync(o => o.Id == stockUpdated.OrderId, stoppingToken);

                        if (order != null)
                        {
                            order.Status = "Confirmed";
                            await context.SaveChangesAsync(stoppingToken);

                            Console.WriteLine($"Pedido {order.Id} confirmado, estoque atualizado.");
                        }
                    }

                    if (_channel != null)
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar StockUpdatedEvent: {ex.Message}");

                    if (_channel != null)
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: "stock-updated",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken
            );

            Console.WriteLine("StockUpdatedConsumer iniciado e ouvindo mensagens...");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.CloseAsync(cancellationToken);
            if (_connection != null) await _connection.CloseAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}
