using System.Net.Http.Json;
using SalesService.DTOs;

namespace SalesService.Services
{
    public class StockApiClient
    {
        private readonly HttpClient _httpClient;

        public StockApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ProductDto?> GetProductAsync(int productId)
        {
            return await _httpClient.GetFromJsonAsync<ProductDto>($"http://localhost:5001/api/products/{productId}");
        }

        public async Task<bool> DecreaseStockAsync(int productId, int quantity)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"http://localhost:5001/api/products/{productId}/decrease",
                new { Quantity = quantity });

            return response.IsSuccessStatusCode;
        }
    }
}