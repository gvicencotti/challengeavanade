namespace Shared.Messaging.Events;

public class StockUpdatedEvent
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int QuantityReduced { get; set; }
    public int NewStock { get; set; }
}