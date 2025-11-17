using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GiaLaiOCOP.Api.Tests.Integration
{
    public class OrdersControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly AppDbContext _context;
        private readonly CustomWebApplicationFactory<Program> _factory;
        private int _customerId;
        private int _productId;

        public OrdersControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();

            var scope = factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            SetupTestData().Wait();
        }

        private async Task SetupTestData()
        {
            // Create Enterprise
            var enterprise = TestHelpers.CreateTestEnterprise();
            _context.Enterprises.Add(enterprise);
            await _context.SaveChangesAsync();

            // Create Customer
            var customer = TestHelpers.CreateTestUser("Customer", "customer@test.com", "Password123!", "Customer");
            _context.Users.Add(customer);
            await _context.SaveChangesAsync();
            _customerId = customer.Id;

            // Create Product
            var product = TestHelpers.CreateTestProduct("Test Product", 100000, enterprise.Id, "Approved");
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            _productId = product.Id;
        }

        [Fact]
        public async Task CreateOrder_WithoutAuth_ShouldReturnUnauthorized()
        {
            // Arrange
            var createDto = new CreateOrderDto
            {
                ShippingAddress = "123 Test Street",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = _productId, Quantity = 1 }
                },
                PaymentMethod = "COD"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/orders", createDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetOrders_WithoutAuth_ShouldReturnUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/orders");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _client.Dispose();
        }
    }
}

