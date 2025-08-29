using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockService.Data;
using StockService.Models;
using StockService.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace StockService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly StockContext _context;

        public ProductsController(StockContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "User,Admin")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var products = await _context.Products.ToListAsync();
            var dtos = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Quantity = p.Quantity,
                Price = p.Price
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "User,Admin")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            var dto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Quantity = product.Quantity,
                Price = product.Price
            };

            return Ok(dto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Quantity = dto.Quantity,
                Price = dto.Price
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var resultDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Quantity = product.Quantity,
                Price = product.Price
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, resultDto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto dto)
        {
            if (id != dto.Id)
                return BadRequest();

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Quantity = dto.Quantity;
            product.Price = dto.Price;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/decrease")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DecreaseStock(int id, [FromBody] DecreaseStockDto dto)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            if (product.Quantity < dto.Quantity)
                return BadRequest($"Estoque insuficiente para o produto {product.Name}.");

            product.Quantity -= dto.Quantity;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}/check")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> CheckStock(int id, [FromQuery] int quantity)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = $"Produto {id} não encontrado." });

            if (quantity <= 0)
                return BadRequest(new { message = "Quantidade deve ser maior que zero." });

            if (product.Quantity < quantity)
            {
                return Conflict(new
                {
                    message = $"Estoque insuficiente para o produto {product.Name}.",
                    available = product.Quantity,
                    requested = quantity
                });
            }

            return Ok(new
            {
                ok = true,
                available = product.Quantity
            });
        }
    }
}
