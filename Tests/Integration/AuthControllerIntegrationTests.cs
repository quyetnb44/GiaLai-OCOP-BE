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
using Microsoft.Extensions.Configuration;

namespace GiaLaiOCOP.Api.Tests.Integration
{
    public class AuthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly AppDbContext _context;
        private readonly CustomWebApplicationFactory<Program> _factory;

        private IServiceScope? _scope;

        public AuthControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();

            // Get DbContext from factory - reuse scope to ensure same database instance
            _scope = factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }

        [Fact]
        public async Task Register_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "Test User",
                Email = "newuser@test.com",
                Password = "Test123456!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            result.Should().NotBeNull();
            result!.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
        {
            // Arrange
            var existingUser = TestHelpers.CreateTestUser("Existing", "existing@test.com");
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterDto
            {
                Name = "New User",
                Email = "existing@test.com",
                Password = "Test123456!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("Test User", "login@test.com", "Test123456!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Verify user was saved correctly
            var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "login@test.com");
            savedUser.Should().NotBeNull();

            var loginDto = new LoginDto
            {
                Email = "login@test.com",
                Password = "Test123456!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Debug: Read response body if failed
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Login failed with status {response.StatusCode}. Response: {errorBody}");
            }

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            result.Should().NotBeNull();
            result!.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("Test User", "login2@test.com", "Test123456!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "login2@test.com",
                Password = "WrongPassword"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithNonExistentEmail_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@test.com",
                Password = "Test123456!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ========== CHANGE PASSWORD TESTS ==========

        [Fact]
        public async Task ChangePassword_WithValidTokenAndPassword_ShouldReturnNewToken()
        {
            // Arrange - Tạo user trực tiếp trong database (giống như test login)
            var user = TestHelpers.CreateTestUser("Test User", "changepwd@test.com", "OldPassword123!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Verify user was saved correctly
            var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "changepwd@test.com");
            savedUser.Should().NotBeNull();

            // Login để lấy token hợp lệ
            _client.DefaultRequestHeaders.Authorization = null;
            var loginDto = new LoginDto
            {
                Email = "changepwd@test.com",
                Password = "OldPassword123!"
            };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            
            if (loginResponse.StatusCode != HttpStatusCode.OK)
            {
                var errorBody = await loginResponse.Content.ReadAsStringAsync();
                throw new Exception($"Login failed with status {loginResponse.StatusCode}. Response: {errorBody}");
            }
            
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            loginResult.Should().NotBeNull();
            var token = loginResult!.Token;
            
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            // Act
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _client.PutAsJsonAsync("/api/auth/change-password", changePasswordDto);

            // Debug: Log response if failed
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"ChangePassword failed with status {response.StatusCode}. Response: {errorBody}. Token: {token.Substring(0, Math.Min(50, token.Length))}...");
            }

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            result.Should().NotBeNull();
            result!.Token.Should().NotBeNullOrEmpty();
            result.Token.Should().NotBe(token); // Token mới phải khác token cũ
            result.Message.Should().Contain("Đổi mật khẩu thành công");

            // Verify PasswordUpdatedAt được cập nhật
            await _context.Entry(savedUser!).ReloadAsync();
            savedUser!.PasswordUpdatedAt.Should().NotBeNull("PasswordUpdatedAt should be set");
            savedUser.PasswordUpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // Verify mật khẩu mới có thể dùng để login
            var newLoginDto = new LoginDto
            {
                Email = "changepwd@test.com",
                Password = "NewPassword123!"
            };
            _client.DefaultRequestHeaders.Authorization = null; // Xóa token cũ
            var newLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", newLoginDto);
            newLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ChangePassword_WithInvalidCurrentPassword_ShouldReturnBadRequest()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("Test User", "invalidpwd@test.com", "CorrectPassword123!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Verify user was saved
            var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "invalidpwd@test.com");
            savedUser.Should().NotBeNull();

            // Login để lấy token hợp lệ
            _client.DefaultRequestHeaders.Authorization = null;
            var loginDto = new LoginDto
            {
                Email = "invalidpwd@test.com",
                Password = "CorrectPassword123!"
            };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            
            if (loginResponse.StatusCode != HttpStatusCode.OK)
            {
                var loginError = await loginResponse.Content.ReadAsStringAsync();
                throw new Exception($"Login failed: {loginError}");
            }
            
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            var token = loginResult!.Token;
            
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "WrongPassword123!",
                NewPassword = "NewPassword123!"
            };

            // Act
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _client.PutAsJsonAsync("/api/auth/change-password", changePasswordDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var errorBody = await response.Content.ReadAsStringAsync();
            errorBody.Should().Contain("Mật khẩu hiện tại không đúng");
        }

        [Fact]
        public async Task ChangePassword_WithInvalidNewPasswordFormat_ShouldReturnBadRequest()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("Test User", "invalidformat@test.com", "OldPassword123!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Login để lấy token hợp lệ
            _client.DefaultRequestHeaders.Authorization = null; // Clear any existing auth
            var loginDto = new LoginDto
            {
                Email = "invalidformat@test.com",
                Password = "OldPassword123!"
            };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            var token = loginResult!.Token;
            
            // Mật khẩu mới không đáp ứng yêu cầu (thiếu chữ hoa, chữ thường hoặc số)
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "weakpassword" // Không có chữ hoa và số
            };

            // Act
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _client.PutAsJsonAsync("/api/auth/change-password", changePasswordDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var errorBody = await response.Content.ReadAsStringAsync();
            (errorBody.Contains("chữ hoa") || errorBody.Contains("chữ thường") || errorBody.Contains("số")).Should().BeTrue();
        }

        [Fact]
        public async Task ChangePassword_WithSamePassword_ShouldReturnBadRequest()
        {
            // Arrange
            var user = TestHelpers.CreateTestUser("Test User", "samepwd@test.com", "SamePassword123!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Verify user was saved
            var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "samepwd@test.com");
            savedUser.Should().NotBeNull();

            // Login để lấy token hợp lệ
            _client.DefaultRequestHeaders.Authorization = null;
            var loginDto = new LoginDto
            {
                Email = "samepwd@test.com",
                Password = "SamePassword123!"
            };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            
            if (loginResponse.StatusCode != HttpStatusCode.OK)
            {
                var loginError = await loginResponse.Content.ReadAsStringAsync();
                throw new Exception($"Login failed: {loginError}");
            }
            
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            var token = loginResult!.Token;
            
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "SamePassword123!",
                NewPassword = "SamePassword123!"
            };

            // Act
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _client.PutAsJsonAsync("/api/auth/change-password", changePasswordDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var errorBody = await response.Content.ReadAsStringAsync();
            errorBody.Should().Contain("Mật khẩu mới phải khác mật khẩu hiện tại");
        }

        [Fact]
        public async Task ChangePassword_WithoutToken_ShouldReturnUnauthorized()
        {
            // Arrange
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            // Act
            _client.DefaultRequestHeaders.Authorization = null;
            var response = await _client.PutAsJsonAsync("/api/auth/change-password", changePasswordDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ChangePassword_WithInvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            // Act
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");
            var response = await _client.PutAsJsonAsync("/api/auth/change-password", changePasswordDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _scope?.Dispose();
            _client.Dispose();
        }
    }
}

