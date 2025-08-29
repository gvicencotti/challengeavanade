namespace SalesService.DTOs

{
    public class OrderDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public List<OrderItemDetailsDto> Items { get; set; }
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}