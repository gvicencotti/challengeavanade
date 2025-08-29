using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using SalesService.Controllers;
using SalesService.Data;
using SalesService.DTOs;
using SalesService.Messaging.Events;
using SalesService.Messaging.Publisher;
using Xunit;

namespace SalesService.Tests
{
    public class OrdersControllerTests
    {
        private static SalesContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<SalesContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new SalesContext(options);
        }

        private static IHttpClientFactory CreateHttpClientFactory(HttpResponseMessage response)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response);

            var client = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://fake-stock-service/")
            };

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                       .Returns(client);

            return factoryMock.Object;
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnCreatedOrder_WhenStockIsAvailable()
        {
            using var context = CreateInMemoryContext();

            var publisherMock = new Mock<IRabbitMqPublisher>();
            publisherMock.Setup(p => p.Publish(It.IsAny<OrderCreatedEvent>()))
                         .Returns(Task.CompletedTask);

            var product = new ProductDto
            {
                Id = 1,
                Name = "Cadeira Gamer",
                Description = "Confortável",
                Quantity = 5,
                Price = 999.90m
            };

            var productJson = JsonSerializer.Serialize(product);

            var stockCheckResponse = new HttpResponseMessage(HttpStatusCode.OK);

            var productDetailsResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(productJson, Encoding.UTF8, "application/json")
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            var callCount = 0;
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() =>
                {
                    callCount++;
                    return callCount == 1 ? stockCheckResponse : productDetailsResponse;
                });

            var client = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://fake-stock-service/") // 🔥 correção
            };

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                       .Returns(client);

            var controller = new OrdersController(context, publisherMock.Object);

            var dto = new CreateOrderDto
            {
                CustomerId = 123,
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 1, Quantity = 1 }
                }
            };

            var result = await controller.CreateOrder(dto, factoryMock.Object);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedOrder = Assert.IsType<OrderDto>(createdResult.Value);

            Assert.Equal(123, returnedOrder.CustomerId);
            Assert.Single(returnedOrder.Items);
            Assert.Equal("Cadeira Gamer", returnedOrder.Items[0].ProductName);
            Assert.Equal(999.90m, returnedOrder.Items[0].Price);
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnNotFound_WhenProductDoesNotExist()
        {
            using var context = CreateInMemoryContext();

            var publisherMock = new Mock<IRabbitMqPublisher>();

            var factory = CreateHttpClientFactory(new HttpResponseMessage(HttpStatusCode.NotFound));

            var controller = new OrdersController(context, publisherMock.Object);

            var dto = new CreateOrderDto
            {
                CustomerId = 1,
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 99, Quantity = 1 }
                }
            };

            var result = await controller.CreateOrder(dto, factory);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnBadRequest_WhenStockIsInsufficient()
        {
            using var context = CreateInMemoryContext();

            var publisherMock = new Mock<IRabbitMqPublisher>();

            var factory = CreateHttpClientFactory(new HttpResponseMessage(HttpStatusCode.Conflict));

            var controller = new OrdersController(context, publisherMock.Object);

            var dto = new CreateOrderDto
            {
                CustomerId = 1,
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 1, Quantity = 999 }
                }
            };

            var result = await controller.CreateOrder(dto, factory);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
    }
}
