using Xunit;
using FluentAssertions;
using GiaLaiOCOP.Api.Services;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GiaLaiOCOP.Api.Tests.Services
{
    public class TokenServiceTests
    {
        private readonly IConfiguration _configuration;
        private readonly TokenService _tokenService;

        public TokenServiceTests()
        {
            // Setup in-memory configuration
            var configDict = new Dictionary<string, string?>
            {
                { "Jwt:Key", "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456789" },
                { "Jwt:Issuer", "GiaLaiOCOP" },
                { "Jwt:Audience", "GiaLaiOCOPUsers" }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            _tokenService = new TokenService(_configuration);
        }

        [Fact]
        public void CreateToken_WithValidInput_ShouldReturnValidToken()
        {
            // Arrange
            var userId = 1;
            var email = "test@example.com";
            var role = "Customer";

            // Act
            var token = _tokenService.CreateToken(userId, email, role);

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Should().NotContain(" ");
        }

        [Fact]
        public void CreateToken_ShouldContainCorrectClaims()
        {
            // Arrange
            var userId = 1;
            var email = "test@example.com";
            var role = "Customer";

            // Act
            var token = _tokenService.CreateToken(userId, email, role);
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // Assert
            jsonToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
            jsonToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
            jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role);
        }

        [Fact]
        public void CreateToken_ShouldHaveCorrectExpiration()
        {
            // Arrange
            var userId = 1;
            var email = "test@example.com";
            var role = "Customer";

            // Act
            var token = _tokenService.CreateToken(userId, email, role);
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // Assert
            jsonToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
            jsonToken.ValidTo.Should().BeBefore(DateTime.UtcNow.AddHours(4)); // Should be around 3 hours
        }

        [Theory]
        [InlineData(1, "customer@test.com", "Customer")]
        [InlineData(2, "admin@test.com", "EnterpriseAdmin")]
        [InlineData(3, "system@test.com", "SystemAdmin")]
        public void CreateToken_WithDifferentRoles_ShouldCreateValidToken(int userId, string email, string role)
        {
            // Act
            var token = _tokenService.CreateToken(userId, email, role);
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // Assert
            jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role);
        }

        [Fact]
        public void CreateToken_ShouldHaveCorrectIssuerAndAudience()
        {
            // Arrange
            var userId = 1;
            var email = "test@example.com";
            var role = "Customer";

            // Act
            var token = _tokenService.CreateToken(userId, email, role);
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // Assert
            jsonToken.Issuer.Should().Be("GiaLaiOCOP");
            jsonToken.Audiences.Should().Contain("GiaLaiOCOPUsers");
        }
    }
}
