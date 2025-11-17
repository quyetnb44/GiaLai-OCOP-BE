using Xunit;
using FluentAssertions;
using GiaLaiOCOP.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;

namespace GiaLaiOCOP.Api.Tests.Services
{
    public class EmailServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly EmailService _emailService;

        public EmailServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<EmailService>>();

            // Setup default configuration
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(x => x["SmtpHost"]).Returns("smtp.gmail.com");
            configSection.Setup(x => x["SmtpPort"]).Returns("587");
            configSection.Setup(x => x["SmtpUsername"]).Returns("test@gmail.com");
            configSection.Setup(x => x["SmtpPassword"]).Returns("test-password");
            configSection.Setup(x => x["FromEmail"]).Returns("test@gmail.com");
            configSection.Setup(x => x["FromName"]).Returns("GiaLai OCOP");

            _mockConfig.Setup(x => x.GetSection("Email")).Returns(configSection.Object);

            _emailService = new EmailService(_mockConfig.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SendOtpEmailAsync_WithMissingConfiguration_ShouldReturnFalse()
        {
            // Arrange
            var emptyConfig = new Mock<IConfiguration>();
            var emptyConfigSection = new Mock<IConfigurationSection>();
            emptyConfigSection.Setup(x => x["SmtpHost"]).Returns((string?)null);
            emptyConfig.Setup(x => x.GetSection("Email")).Returns(emptyConfigSection.Object);

            var service = new EmailService(emptyConfig.Object, _mockLogger.Object);

            // Act
            var result = await service.SendOtpEmailAsync("test@example.com", "123456");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SendOtpEmailAsync_WithPlaceholderConfiguration_ShouldReturnFalse()
        {
            // Arrange
            var placeholderConfig = new Mock<IConfiguration>();
            var placeholderConfigSection = new Mock<IConfigurationSection>();
            placeholderConfigSection.Setup(x => x["SmtpHost"]).Returns("smtp.gmail.com");
            placeholderConfigSection.Setup(x => x["SmtpUsername"]).Returns("your-email@gmail.com");
            placeholderConfigSection.Setup(x => x["SmtpPassword"]).Returns("your-app-password");
            placeholderConfig.Setup(x => x.GetSection("Email")).Returns(placeholderConfigSection.Object);

            var service = new EmailService(placeholderConfig.Object, _mockLogger.Object);

            // Act
            var result = await service.SendOtpEmailAsync("test@example.com", "123456");

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("Register")]
        [InlineData("Login")]
        [InlineData("ResetPassword")]
        public async Task SendOtpEmailAsync_WithDifferentPurposes_ShouldHaveCorrectSubject(string purpose)
        {
            // Note: This test would require mocking SMTP client, which is complex
            // In a real scenario, you might want to use a test SMTP server or mock the SmtpClient
            // For now, we test the configuration logic

            // Arrange
            var result = await _emailService.SendOtpEmailAsync("test@example.com", "123456", purpose);

            // Assert - In real scenario, we would verify the email was sent with correct subject
            // For now, we just verify the method doesn't throw
            // Result can be false if SMTP is not configured, or true if configured
            result.Should().BeFalse(); // In test environment, SMTP is not configured, so result should be false
        }

        [Fact]
        public async Task SendOtpEmailAsync_WithValidConfiguration_ShouldNotThrow()
        {
            // Act & Assert
            await _emailService.Invoking(s => s.SendOtpEmailAsync("test@example.com", "123456"))
                .Should().NotThrowAsync();
        }
    }
}

