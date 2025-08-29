namespace Shared.Messaging.Events;

public class OrderItemEvent
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
