using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace GiaLaiOCOP.Api.Tests.Helpers
{
    /// <summary>
    /// Helper methods cho testing
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Tạo in-memory database cho testing
        /// </summary>
        public static AppDbContext CreateInMemoryDbContext(string? databaseName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName ?? Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        /// <summary>
        /// Tạo test user
        /// </summary>
        public static User CreateTestUser(
            string name = "Test User",
            string email = "test@example.com",
            string password = "Test123456!",
            string role = "Customer",
            int? enterpriseId = null)
        {
            return new User
            {
                Name = name,
                Email = email.Trim().ToLower(), // Ensure email is lowercase to match login logic
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                EnterpriseId = enterpriseId,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Tạo test enterprise
        /// </summary>
        public static Enterprise CreateTestEnterprise(
            string name = "Test Enterprise",
            int? ocopRating = 5)
        {
            return new Enterprise
            {
                Name = name,
                Description = "Test Description",
                Address = "123 Test Street",
                District = "Test District",
                Province = "Gia Lai",
                OCOPRating = ocopRating,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Tạo test product
        /// </summary>
        public static Product CreateTestProduct(
            string name = "Test Product",
            decimal price = 100000,
            int enterpriseId = 1,
            string status = "Approved")
        {
            return new Product
            {
                Name = name,
                Description = "Test Description",
                Price = price,
                EnterpriseId = enterpriseId,
                Status = status,
                StockStatus = "InStock",
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Tạo test order
        /// </summary>
        public static Order CreateTestOrder(
            int userId = 1,
            decimal totalAmount = 100000,
            string status = "Pending")
        {
            return new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                Status = status,
                PaymentMethod = "COD",
                PaymentStatus = "Pending"
            };
        }

        /// <summary>
        /// Tạo test review
        /// </summary>
        public static Review CreateTestReview(
            int userId = 1,
            int productId = 1,
            int rating = 5,
            string comment = "Great product!")
        {
            return new Review
            {
                UserId = userId,
                ProductId = productId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Setup ControllerBase với user claims
        /// </summary>
        public static void SetupControllerContext(ControllerBase controller, int userId, string email, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        /// <summary>
        /// Seed test data vào database
        /// </summary>
        public static async Task SeedTestDataAsync(AppDbContext context)
        {
            // Tạo Enterprise
            var enterprise = CreateTestEnterprise();
            context.Enterprises.Add(enterprise);
            await context.SaveChangesAsync();

            // Tạo Users
            var customer = CreateTestUser("Customer", "customer@test.com", "Password123!", "Customer");
            var enterpriseAdmin = CreateTestUser("Enterprise Admin", "admin@test.com", "Password123!", "EnterpriseAdmin", enterprise.Id);
            var systemAdmin = CreateTestUser("System Admin", "system@test.com", "Password123!", "SystemAdmin");

            context.Users.AddRange(customer, enterpriseAdmin, systemAdmin);
            await context.SaveChangesAsync();

            // Tạo Products
            var product1 = CreateTestProduct("Product 1", 100000, enterprise.Id, "Approved");
            var product2 = CreateTestProduct("Product 2", 200000, enterprise.Id, "PendingApproval");
            context.Products.AddRange(product1, product2);
            await context.SaveChangesAsync();

            // Tạo Reviews
            var review1 = CreateTestReview(customer.Id, product1.Id, 5, "Excellent!");
            var review2 = CreateTestReview(customer.Id, product1.Id, 4, "Good!");
            context.Reviews.AddRange(review1, review2);
            await context.SaveChangesAsync();
        }
    }
}
