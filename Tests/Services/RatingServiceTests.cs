using Xunit;
using FluentAssertions;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GiaLaiOCOP.Api.Tests.Services
{
    public class RatingServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly RatingService _ratingService;

        public RatingServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _ratingService = new RatingService(_context);
        }

        [Fact]
        public async Task UpdateProductAverageRatingAsync_WithReviews_ShouldCalculateCorrectAverage()
        {
            // Arrange
            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Description = "Test",
                Price = 100000,
                EnterpriseId = 1,
                Status = "Approved",
                Reviews = new List<Review>
                {
                    new Review { Id = 1, UserId = 1, ProductId = 1, Rating = 5, Comment = "Great" },
                    new Review { Id = 2, UserId = 2, ProductId = 1, Rating = 4, Comment = "Good" },
                    new Review { Id = 3, UserId = 3, ProductId = 1, Rating = 3, Comment = "OK" }
                }
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            await _ratingService.UpdateProductAverageRatingAsync(1);

            // Assert
            var updatedProduct = await _context.Products.FindAsync(1);
            updatedProduct.Should().NotBeNull();
            updatedProduct!.AverageRating.Should().Be(4.0); // (5 + 4 + 3) / 3 = 4.0
        }

        [Fact]
        public async Task UpdateProductAverageRatingAsync_WithoutReviews_ShouldSetNull()
        {
            // Arrange
            var product = new Product
            {
                Id = 2,
                Name = "Test Product 2",
                Description = "Test",
                Price = 100000,
                EnterpriseId = 1,
                Status = "Approved",
                Reviews = new List<Review>()
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            await _ratingService.UpdateProductAverageRatingAsync(2);

            // Assert
            var updatedProduct = await _context.Products.FindAsync(2);
            updatedProduct.Should().NotBeNull();
            updatedProduct!.AverageRating.Should().BeNull();
        }

        [Fact]
        public async Task UpdateProductAverageRatingAsync_ProductNotFound_ShouldNotThrow()
        {
            // Act & Assert
            await _ratingService.Invoking(s => s.UpdateProductAverageRatingAsync(999))
                .Should().NotThrowAsync();
        }

        [Fact]
        public async Task UpdateEnterpriseAverageRatingAsync_WithApprovedProducts_ShouldCalculateCorrectAverage()
        {
            // Arrange
            var enterprise = new Enterprise
            {
                Id = 1,
                Name = "Test Enterprise",
                Products = new List<Product>
                {
                    new Product { Id = 1, Name = "Product 1", EnterpriseId = 1, Status = "Approved", AverageRating = 5.0 },
                    new Product { Id = 2, Name = "Product 2", EnterpriseId = 1, Status = "Approved", AverageRating = 4.0 },
                    new Product { Id = 3, Name = "Product 3", EnterpriseId = 1, Status = "PendingApproval", AverageRating = 3.0 } // Không tính
                }
            };

            _context.Enterprises.Add(enterprise);
            await _context.SaveChangesAsync();

            // Act
            await _ratingService.UpdateEnterpriseAverageRatingAsync(1);

            // Assert
            var updatedEnterprise = await _context.Enterprises.FindAsync(1);
            updatedEnterprise.Should().NotBeNull();
            updatedEnterprise!.AverageRating.Should().Be(4.5); // (5.0 + 4.0) / 2 = 4.5
        }

        [Fact]
        public async Task UpdateEnterpriseAverageRatingAsync_WithoutApprovedProducts_ShouldSetNull()
        {
            // Arrange
            var enterprise = new Enterprise
            {
                Id = 2,
                Name = "Test Enterprise 2",
                Products = new List<Product>
                {
                    new Product { Id = 4, Name = "Product 4", EnterpriseId = 2, Status = "PendingApproval", AverageRating = 5.0 }
                }
            };

            _context.Enterprises.Add(enterprise);
            await _context.SaveChangesAsync();

            // Act
            await _ratingService.UpdateEnterpriseAverageRatingAsync(2);

            // Assert
            var updatedEnterprise = await _context.Enterprises.FindAsync(2);
            updatedEnterprise.Should().NotBeNull();
            updatedEnterprise!.AverageRating.Should().BeNull();
        }

        [Fact]
        public async Task UpdateEnterpriseAverageRatingAsync_EnterpriseNotFound_ShouldNotThrow()
        {
            // Act & Assert
            await _ratingService.Invoking(s => s.UpdateEnterpriseAverageRatingAsync(999))
                .Should().NotThrowAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

