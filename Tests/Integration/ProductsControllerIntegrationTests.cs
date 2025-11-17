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
    public class ProductsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly AppDbContext _context;
        private readonly CustomWebApplicationFactory<Program> _factory;
        private int _enterpriseId;

        public ProductsControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
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
            _enterpriseId = enterprise.Id;

            // Create Users
            var customer = TestHelpers.CreateTestUser("Customer", "customer@test.com", "Password123!", "Customer");
            var enterpriseAdmin = TestHelpers.CreateTestUser("Enterprise Admin", "admin@test.com", "Password123!", "EnterpriseAdmin", enterprise.Id);
            _context.Users.AddRange(customer, enterpriseAdmin);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetProducts_WithoutAuth_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/products");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetProduct_WithValidId_ShouldReturnProduct()
        {
            // Arrange - Ensure enterprise exists and product has proper relationship
            var enterprise = await _context.Enterprises.FindAsync(_enterpriseId);
            enterprise.Should().NotBeNull();

            var product = TestHelpers.CreateTestProduct("Test Product", 100000, _enterpriseId, "Approved");
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Verify product was saved correctly with enterprise relationship
            var savedProduct = await _context.Products
                .Include(p => p.Enterprise)
                .FirstOrDefaultAsync(p => p.Id == product.Id);
            savedProduct.Should().NotBeNull();
            savedProduct!.Status.Should().Be("Approved");
            savedProduct.EnterpriseId.Should().Be(_enterpriseId);

            // Act - Use a fresh scope to simulate HTTP request
            var response = await _client.GetAsync($"/api/products/{product.Id}");

            // Debug: Read response body if failed
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                // Also check if product exists in a fresh context
                using var freshScope = _factory.Services.CreateScope();
                var freshContext = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var productInFreshContext = await freshContext.Products.FindAsync(product.Id);
                throw new Exception($"GetProduct failed with status {response.StatusCode}. Response: {errorBody}. Product exists in fresh context: {productInFreshContext != null}");
            }

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ProductDto>();
            result.Should().NotBeNull();
            result!.Name.Should().Be("Test Product");
        }

        [Fact]
        public async Task GetProduct_WithInvalidId_ShouldReturnNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/products/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateProduct_WithoutAuth_ShouldReturnUnauthorized()
        {
            // Arrange - Using anonymous object
            var createDto = new
            {
                name = "New Product",
                description = "Description",
                price = 100000
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/products", createDto);

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

