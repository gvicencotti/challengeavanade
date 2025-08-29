using Microsoft.EntityFrameworkCore;
using SalesService.Data;
using SalesService.DTOs;

namespace SalesService.Services
{
    public class OrderService
    {
        private readonly SalesContext _db;
        private readonly StockApiClient _stockApi;

        public OrderService(SalesContext db, StockApiClient stockApi)
        {
            _db = db;
            _stockApi = stockApi;
        }

        public async Task<List<OrderDto>> GetOrdersAsync()
        {
            var orders = await _db.Orders.Include(o => o.Items).ToListAsync();
            var result = new List<OrderDto>();

            foreach (var order in orders)
            {
                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    Items = new List<OrderItemDetailsDto>()
                };

                foreach (var item in order.Items)
                {
                    var product = await _stockApi.GetProductAsync(item.ProductId);

                    orderDto.Items.Add(new OrderItemDetailsDto
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        ProductName = product?.Name ?? "Desconhecido",
                        Price = product?.Price ?? 0,
                        Quantity = item.Quantity
                    });
                }

                result.Add(orderDto);
            }

            return result;
        }
    }
}