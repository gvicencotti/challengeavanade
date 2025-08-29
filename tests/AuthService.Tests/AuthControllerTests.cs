using Xunit;
using AuthService.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Tests
{
    public class AuthControllerTests
    {
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            var inMemorySettings = new Dictionary<string, string> {
                { "Jwt:Key", "SuperSecretKey_ThatIsLongEnough_123456789" },
                { "Jwt:Issuer", "AuthService" },
                { "Jwt:Audience", "AuthService" }
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            _controller = new AuthController(config);
        }

        [Fact]
        public void Login_WithValidAdminCredentials_ReturnsToken()
        {
            var request = new LoginRequest("admin", "123");
            var result = _controller.Login(request) as OkObjectResult;

            Assert.NotNull(result);
            var tokenObj = result.Value!.GetType().GetProperty("token")!.GetValue(result.Value, null);
            Assert.NotNull(tokenObj);
            Assert.IsType<string>(tokenObj);
        }

        [Fact]
        public void Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            var request = new LoginRequest("invalid", "wrong");
            var result = _controller.Login(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void Login_WithValidUserCredentials_ReturnsToken()
        {
            var request = new LoginRequest("user", "123");
            var result = _controller.Login(request) as OkObjectResult;

            Assert.NotNull(result);
            var tokenObj = result.Value!.GetType().GetProperty("token")!.GetValue(result.Value, null);
            Assert.NotNull(tokenObj);
            Assert.IsType<string>(tokenObj);
        }
    }
}
