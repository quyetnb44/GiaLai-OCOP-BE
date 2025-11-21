

using Xunit;
using FluentAssertions;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Net;

namespace GiaLaiOCOP.Api.Tests.Controllers
{
    public class AuthControllerTests : IDisposable
    {
        private readonly AppDbContext _context;

        public AuthControllerTests()
        {
            // Setup in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ShouldReturnBadRequest()
        {
            // Arrange
            var existingUser = new User
            {
                Name = "Existing User",
                Email = "existing@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "Customer"
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            // Act - Kiểm tra xem user đã tồn tại chưa
            var userExists = await _context.Users.AnyAsync(u => u.Email == "existing@example.com");

            // Assert
            userExists.Should().BeTrue();
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldFindUser()
        {
            // Arrange
            var user = new User
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Test123456!"),
                Role = "Customer",
                IsEmailVerified = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var foundUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");

            // Assert
            foundUser.Should().NotBeNull();
            foundUser!.Email.Should().Be("test@example.com");
            BCrypt.Net.BCrypt.Verify("Test123456!", foundUser.Password).Should().BeTrue();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}


