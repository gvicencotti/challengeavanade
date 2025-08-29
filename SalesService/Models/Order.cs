namespace SalesService.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CustomerName { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public List<OrderItem> Items { get; set; } = new();
        public int CustomerId { get; set; }
    }
    

}
