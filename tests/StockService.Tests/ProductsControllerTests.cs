using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockService.Controllers;
using StockService.Data;
using StockService.DTOs;
using Xunit;

namespace StockService.Tests
{
    public class ProductsControllerTests
    {
        private static StockContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<StockContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new StockContext(options);
        }

        [Fact]
        public async Task AddProduct_ShouldReturnCreatedProduct()
        {
            using var context = CreateInMemoryContext();
            var controller = new ProductsController(context);

            var productDto = new CreateProductDto
            {
                Name = "Mouse",
                Description = "Mouse gamer",
                Quantity = 10,
                Price = 99.90m
            };

            var actionResult = await controller.CreateProduct(productDto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var returned = Assert.IsType<ProductDto>(createdResult.Value);
            Assert.Equal("Mouse", returned.Name);
            Assert.Equal(10, returned.Quantity);
        }

        [Fact]
        public async Task GetProduct_ShouldReturnCorrectProduct()
        {
            using var context = CreateInMemoryContext();
            var controller = new ProductsController(context);

            var productDto = new CreateProductDto
            {
                Name = "Teclado",
                Description = "Teclado mecânico",
                Quantity = 5,
                Price = 199.90m
            };

            var createdAction = await controller.CreateProduct(productDto);
            var createdResult = Assert.IsType<CreatedAtActionResult>(createdAction.Result);
            var createdProduct = Assert.IsType<ProductDto>(createdResult.Value);

            var getAction = await controller.GetProduct(createdProduct.Id);

            var okResult = Assert.IsType<OkObjectResult>(getAction.Result);
            var returned = Assert.IsType<ProductDto>(okResult.Value);
            Assert.Equal("Teclado", returned.Name);
            Assert.Equal(createdProduct.Id, returned.Id);
        }

        [Fact]
        public async Task UpdateStock_ShouldReduceQuantity_WhenDecreaseStockCalled()
        {
            using var context = CreateInMemoryContext();
            var controller = new ProductsController(context);

            var productDto = new CreateProductDto
            {
                Name = "Monitor",
                Description = "Monitor Full HD",
                Quantity = 2,
                Price = 799.90m
            };

            var createdAction = await controller.CreateProduct(productDto);
            var createdResult = Assert.IsType<CreatedAtActionResult>(createdAction.Result);
            var createdProduct = Assert.IsType<ProductDto>(createdResult.Value);

            var decreaseDto = new DecreaseStockDto { Quantity = 1 };

            var decreaseAction = await controller.DecreaseStock(createdProduct.Id, decreaseDto);

            Assert.IsType<NoContentResult>(decreaseAction);

            var getAction = await controller.GetProduct(createdProduct.Id);
            var okResult = Assert.IsType<OkObjectResult>(getAction.Result);
            var returned = Assert.IsType<ProductDto>(okResult.Value);

            Assert.Equal(createdProduct.Quantity - 1, returned.Quantity);
        }
    }
}
