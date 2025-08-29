namespace SalesService.Messaging.Events
{
    public class OrderCreatedEvent
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public List<OrderItemEvent> Items { get; set; }
    }

    public class OrderItemEvent
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
