namespace Shared.Messaging.Events;

public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public List<OrderItemEvent> Items { get; set; }
}
