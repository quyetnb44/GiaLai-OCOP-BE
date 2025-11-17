using Xunit;
using FluentAssertions;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GiaLaiOCOP.Api.Tests.Controllers
{
    public class ProductsControllerTests : IDisposable
    {
        private readonly AppDbContext _context;

        public ProductsControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
        }

        [Fact]
        public async Task GetProducts_ShouldReturnProducts()
        {
            // Arrange
            var product = new Product
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 100000,
                EnterpriseId = 1,
                Status = "Approved"
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var products = await _context.Products.ToListAsync();

            // Assert
            products.Should().NotBeNull();
            products.Should().HaveCount(1);
            products[0].Name.Should().Be("Test Product");
        }

        [Fact]
        public async Task GetProduct_WithValidId_ShouldReturnProduct()
        {
            // Arrange
            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Description = "Test Description",
                Price = 100000,
                EnterpriseId = 1,
                Status = "Approved"
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var foundProduct = await _context.Products.FindAsync(1);

            // Assert
            foundProduct.Should().NotBeNull();
            foundProduct!.Name.Should().Be("Test Product");
        }

        [Fact]
        public async Task GetProduct_WithInvalidId_ShouldReturnNull()
        {
            // Act
            var product = await _context.Products.FindAsync(999);

            // Assert
            product.Should().BeNull();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

