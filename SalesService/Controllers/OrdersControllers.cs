using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesService.Data;
using SalesService.Models;
using SalesService.DTOs;
using SalesService.Messaging.Events;
using SalesService.Messaging.Publisher;
using System.Text.Json;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace SalesService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly SalesContext _context;
        private readonly IRabbitMqPublisher _publisher;

        public OrdersController(SalesContext context, IRabbitMqPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .ToListAsync();

            var dtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                Items = o.Items.Select(i => new OrderItemDetailsDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "User,Admin")]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var orderDto = new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Items = order.Items.Select(i => new OrderItemDetailsDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };

            return Ok(orderDto);
        }

        [HttpPost]
        [Authorize(Roles = "User, Admin")]
        public async Task<ActionResult<OrderDto>> CreateOrder(
            CreateOrderDto dto,
            [FromServices] IHttpClientFactory httpClientFactory)
        {
            var client = httpClientFactory.CreateClient("StockService");

            if (Request.Headers.ContainsKey("Authorization"))
            {
                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.Add("Authorization", Request.Headers["Authorization"].ToString());
            }

            foreach (var item in dto.Items)
            {
                var response = await client.GetAsync($"products/{item.ProductId}/check?quantity={item.Quantity}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound(new { message = $"Produto {item.ProductId} não encontrado." });
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    return BadRequest(new { message = $"Produto {item.ProductId} não possui estoque suficiente para atender a quantidade {item.Quantity}." });
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(502, new
                    {
                        message = $"Falha ao consultar estoque do produto {item.ProductId}.",
                        statusCode = (int)response.StatusCode
                    });
                }
            }

            var order = new Order
            {
                CustomerId = dto.CustomerId,
                Status = "Pending",
                Items = dto.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                Items = order.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };

            await _publisher.Publish(orderCreatedEvent);

            var result = new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Items = new List<OrderItemDetailsDto>()
            };

            foreach (var item in order.Items)
            {
                var response = await client.GetAsync($"products/{item.ProductId}");
                ProductDto? product = null;

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    product = JsonSerializer.Deserialize<ProductDto>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }

                result.Items.Add(new OrderItemDetailsDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = product?.Name,
                    Price = product?.Price ?? 0,
                    Quantity = item.Quantity,
                    Total = (product?.Price ?? 0) * item.Quantity
                });
            }

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, result);
        }
    }
}
