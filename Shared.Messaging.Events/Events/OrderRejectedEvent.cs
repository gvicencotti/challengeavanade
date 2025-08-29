namespace Shared.Messaging.Events;

public class OrderRejectedEvent
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int RequestedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public string Reason { get; set; }
}