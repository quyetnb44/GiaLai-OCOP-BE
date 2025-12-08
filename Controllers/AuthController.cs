using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Services;
using Microsoft.AspNetCore.Identity; 

namespace GiaLaiOCOP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly ISocialAuthService _socialAuthService;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthController(AppDbContext context, IConfiguration config, IEmailService emailService, ILogger<AuthController> logger, ISocialAuthService socialAuthService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
            _logger = logger;
            _socialAuthService = socialAuthService;
        }

        // 🔹 Helper: Tạo mã OTP 6 chữ số
        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // 🔹 Helper: Xóa OTP cũ đã hết hạn hoặc đã sử dụng
        private async Task CleanupOldOtpsAsync(string email, string purpose)
        {
            var expiredOtps = await _context.EmailVerifications
                .Where(e => e.Email == email && 
                           e.Purpose == purpose && 
                           (e.ExpiresAt < DateTime.UtcNow || e.IsUsed))
                .ToListAsync();

            _context.EmailVerifications.RemoveRange(expiredOtps);
            await _context.SaveChangesAsync();
        }

        private async Task<User?> GetUserFromClaimsAsync()
        {
            try
            {
                // Log tất cả claims để debug
                var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
                _logger.LogInformation($"GetUserFromClaimsAsync - All claims: {string.Join(", ", allClaims)}");

                // Ưu tiên tìm bằng UserId (ClaimTypes.NameIdentifier)
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation($"GetUserFromClaimsAsync - UserIdClaim: {userIdClaim}");
                
                if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogInformation($"GetUserFromClaimsAsync - Parsed userId: {userId}, searching in database...");
                    
                    // Đếm tổng số users trong database để debug
                    var totalUsers = await _context.Users.CountAsync();
                    _logger.LogInformation($"GetUserFromClaimsAsync - Total users in database: {totalUsers}");
                    
                    // Luôn dùng FirstOrDefaultAsync để đảm bảo hoạt động với cả in-memory và real database
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user != null)
                    {
                        _logger.LogInformation($"GetUserFromClaimsAsync - ✅ Found user by ID: {userId}, Email: {user.Email}");
                        return user;
                    }
                    
                    // Nếu không tìm thấy, log tất cả user IDs để debug
                    var allUserIds = await _context.Users.Select(u => u.Id).ToListAsync();
                    _logger.LogWarning($"GetUserFromClaimsAsync - ❌ User not found by ID: {userId}. Available user IDs: {string.Join(", ", allUserIds)}");
                }

                // Fallback: Tìm bằng email (JwtRegisteredClaimNames.Sub hoặc ClaimTypes.Email)
                var emailClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                                 ?? User.FindFirst(ClaimTypes.Email)?.Value;
                
                _logger.LogInformation($"GetUserFromClaimsAsync - EmailClaim: {emailClaim}");

                if (!string.IsNullOrWhiteSpace(emailClaim))
                {
                    var emailLower = emailClaim.Trim().ToLower();
                    _logger.LogInformation($"GetUserFromClaimsAsync - Searching by email (lowercase): {emailLower}");
                    
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);
                    if (user != null)
                    {
                        _logger.LogInformation($"GetUserFromClaimsAsync - ✅ Found user by email: {emailLower}, ID: {user.Id}");
                        return user;
                    }
                    
                    // Nếu không tìm thấy, log tất cả emails để debug
                    var allEmails = await _context.Users.Select(u => u.Email).ToListAsync();
                    _logger.LogWarning($"GetUserFromClaimsAsync - ❌ User not found by email: {emailLower}. Available emails: {string.Join(", ", allEmails)}");
                }
                else
                {
                    _logger.LogWarning("GetUserFromClaimsAsync - No email claim found");
                }

                _logger.LogError("GetUserFromClaimsAsync - ❌ User not found by any method");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserFromClaimsAsync - Exception occurred");
                return null;
            }
        }

        private string GetJwtKey()
        {
            var key = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("JWT key is not configured.");
            }
            return key;
        }

        private bool VerifyPassword(User user, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(user.Password) || string.IsNullOrWhiteSpace(providedPassword))
            {
                return false;
            }

            // Kiểm tra xem password hash có phải là BCrypt format không (bắt đầu với $2a$, $2b$, $2x$, $2y$)
            var isBcryptHash = user.Password.StartsWith("$2a$") || 
                              user.Password.StartsWith("$2b$") || 
                              user.Password.StartsWith("$2x$") || 
                              user.Password.StartsWith("$2y$");

            if (isBcryptHash)
            {
                // Password được hash bằng BCrypt
                try
                {
                    return BCrypt.Net.BCrypt.Verify(providedPassword, user.Password);
                }
                catch (BCrypt.Net.SaltParseException ex)
                {
                    _logger.LogWarning($"BCrypt verification failed for user {user.Id}: {ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"BCrypt verification error for user {user.Id}: {ex.Message}");
                    return false;
                }
            }
            else
            {
                // Password được hash bằng PasswordHasher (ASP.NET Identity)
                try
                {
                    var result = _passwordHasher.VerifyHashedPassword(user, user.Password, providedPassword);
                    return result == PasswordVerificationResult.Success || 
                           result == PasswordVerificationResult.SuccessRehashNeeded;
                }
                catch (FormatException ex)
                {
                    _logger.LogWarning($"PasswordHasher verification failed (FormatException) for user {user.Id}: {ex.Message}");
                    // Fallback: thử BCrypt nếu PasswordHasher fail
                    try
                    {
                        return BCrypt.Net.BCrypt.Verify(providedPassword, user.Password);
                    }
                    catch
                    {
                        return false;
                    }
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning($"PasswordHasher verification failed (ArgumentException) for user {user.Id}: {ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"PasswordHasher verification error for user {user.Id}: {ex.Message}");
                    return false;
                }
            }
        }

        // 🔹 POST /api/auth/register - ĐĂNG KÝ (Không cần OTP)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();

            // Kiểm tra email đã tồn tại
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
                return Conflict("Email đã được sử dụng.");

            // Tạo user mới (không cần OTP, IsEmailVerified = false)
            var user = new User
            {
                Name = dto.Name.Trim(),
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Customer",
                IsEmailVerified = false // Không bắt buộc xác thực email
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 🔹 Tự động tạo ví cho user mới đăng ký
            var wallet = new Wallet
            {
                UserId = user.Id,
                Balance = 0,
                Currency = "VND",
                CreatedAt = DateTime.UtcNow
            };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            // Tạo JWT token cho user mới đăng ký - đảm bảo email được normalize
            var normalizedEmail = user.Email.Trim().ToLower();
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, normalizedEmail), // Dùng email đã normalize
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:TokenLifetimeMinutes"] ?? "60"));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return CreatedAtAction(nameof(Register), new { id = user.Id }, new AuthResponseDto 
            { 
                Token = tokenString, 
                Expires = expires,
                Message = "Đăng ký thành công."
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // 🔹 Kiểm tra validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null) return Unauthorized("Email hoặc mật khẩu không đúng.");

            // 🔹 Kiểm tra password
            if (!VerifyPassword(user, dto.Password))
                return Unauthorized("Email hoặc mật khẩu không đúng.");

            // 🔹 Kiểm tra tài khoản có bị vô hiệu hóa không
            if (!user.IsActive)
                return Unauthorized("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");

            // 🔹 Bỏ kiểm tra email verification - cho phép đăng nhập dù email chưa xác thực
            // Email verification chỉ là optional, không bắt buộc để đăng nhập

            // tạo claims - đảm bảo email được normalize để tránh vấn đề case sensitivity
            var normalizedEmail = user.Email.Trim().ToLower();
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, normalizedEmail), // Dùng email đã normalize
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:TokenLifetimeMinutes"] ?? "60"));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new AuthResponseDto { Token = tokenString, Expires = expires });
        }

        // 🔹 Helper: Tạo JWT token cho user và trả về token cùng với thời gian hết hạn
        private (string Token, DateTime Expires) GenerateJwtToken(User user)
        {
            var normalizedEmail = user.Email.Trim().ToLower();
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, normalizedEmail),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:TokenLifetimeMinutes"] ?? "60"));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return (tokenString, expires);
        }

        // 🔹 PUT /api/auth/change-password - Đổi mật khẩu cho user đã đăng nhập
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                // 1. Kiểm tra ModelState validation
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // 2. Log tất cả claims để debug
                var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
                _logger.LogInformation($"ChangePassword - All claims from token: {string.Join(", ", allClaims)}");

                // 3. Lấy user từ JWT token - thử nhiều cách
                User? user = null;

                // Cách 1: Tìm bằng UserId từ ClaimTypes.NameIdentifier
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogInformation($"ChangePassword - Trying to find user by ID: {userId}");
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user != null)
                    {
                        _logger.LogInformation($"ChangePassword - ✅ Found user by ID: {userId}, Email: {user.Email}");
                    }
                }

                // Cách 2: Tìm bằng email từ JwtRegisteredClaimNames.Sub
                if (user == null)
                {
                    var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                    if (!string.IsNullOrWhiteSpace(subClaim))
                    {
                        var emailLower = subClaim.Trim().ToLower();
                        _logger.LogInformation($"ChangePassword - Trying to find user by email (Sub claim): {emailLower}");
                        user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);
                        if (user != null)
                        {
                            _logger.LogInformation($"ChangePassword - ✅ Found user by email (Sub): {emailLower}, ID: {user.Id}");
                        }
                    }
                }

                // Cách 3: Tìm bằng email từ ClaimTypes.Email
                if (user == null)
                {
                    var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
                    if (!string.IsNullOrWhiteSpace(emailClaim))
                    {
                        var emailLower = emailClaim.Trim().ToLower();
                        _logger.LogInformation($"ChangePassword - Trying to find user by email (Email claim): {emailLower}");
                        user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);
                        if (user != null)
                        {
                            _logger.LogInformation($"ChangePassword - ✅ Found user by email (Email claim): {emailLower}, ID: {user.Id}");
                        }
                    }
                }

                // Cách 4: Tìm bằng tất cả claims có chứa @ (email)
                if (user == null)
                {
                    foreach (var claim in User.Claims)
                    {
                        if (claim.Value.Contains("@"))
                        {
                            var emailLower = claim.Value.Trim().ToLower();
                            _logger.LogInformation($"ChangePassword - Trying to find user by email from claim {claim.Type}: {emailLower}");
                            user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);
                            if (user != null)
                            {
                                _logger.LogInformation($"ChangePassword - ✅ Found user by email from claim {claim.Type}: {emailLower}, ID: {user.Id}");
                                break;
                            }
                        }
                    }
                }

                // Nếu vẫn không tìm thấy user
                if (user == null)
                {
                    _logger.LogError($"ChangePassword - ❌ User not found from any claims. Available users: {await _context.Users.CountAsync()}");
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại." });
                }

                _logger.LogInformation($"ChangePassword - Processing for user ID: {user.Id}, Email: {user.Email}");

                // 4. Kiểm tra mật khẩu hiện tại
                if (!VerifyPassword(user, dto.CurrentPassword))
                {
                    _logger.LogWarning($"ChangePassword - Invalid current password for user: {user.Email}");
                    return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });
                }

                // 5. Kiểm tra mật khẩu xác nhận khớp với mật khẩu mới
                if (dto.NewPassword != dto.ConfirmNewPassword)
                {
                    _logger.LogWarning($"ChangePassword - Password confirmation mismatch for user: {user.Email}");
                    return BadRequest(new { message = "Mật khẩu xác nhận không khớp với mật khẩu mới" });
                }

                // 6. Validate mật khẩu mới (kiểm tra lại vì có thể bypass ModelState)
                if (string.IsNullOrWhiteSpace(dto.NewPassword))
                {
                    return BadRequest(new { message = "Mật khẩu mới không được để trống" });
                }

                if (dto.NewPassword.Length < 6 || dto.NewPassword.Length > 100)
                {
                    return BadRequest(new { message = "Mật khẩu mới phải có từ 6 đến 100 ký tự" });
                }

                // Kiểm tra format: phải có chữ hoa, chữ thường và số
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$"))
                {
                    return BadRequest(new { message = "Mật khẩu mới phải chứa ít nhất một chữ hoa, một chữ thường và một số" });
                }

                // 7. Kiểm tra mật khẩu mới phải khác mật khẩu hiện tại
                if (VerifyPassword(user, dto.NewPassword))
                {
                    return BadRequest(new { message = "Mật khẩu mới phải khác mật khẩu hiện tại" });
                }

                // 8. Hash mật khẩu mới
                user.Password = _passwordHasher.HashPassword(user, dto.NewPassword);

                // 9. Cập nhật PasswordUpdatedAt để invalidate các token cũ
                user.PasswordUpdatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                // 10. Lưu vào database
                await _context.SaveChangesAsync();
                _logger.LogInformation($"ChangePassword - Password updated successfully for user: {user.Email}");

                // 11. Tạo JWT token mới
                var (newToken, expires) = GenerateJwtToken(user);

                _logger.LogInformation($"ChangePassword - New token generated for user: {user.Email}");

                // 12. Trả về token mới cho FE
                return Ok(new AuthResponseDto
                {
                    Token = newToken,
                    Expires = expires,
                    Message = "Đổi mật khẩu thành công. Vui lòng lưu token mới để tiếp tục sử dụng."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ChangePassword - Exception occurred. Claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi đổi mật khẩu. Vui lòng thử lại sau." });
            }
        }

        // 🔹 POST /api/auth/send-otp - Gửi mã OTP đến email
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();
            var purpose = dto.Purpose ?? "Register";

            // Kiểm tra rate limiting: không cho gửi quá nhiều OTP trong 1 phút
            var recentOtp = await _context.EmailVerifications
                .Where(e => e.Email == email && 
                           e.Purpose == purpose && 
                           e.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
                .FirstOrDefaultAsync();

            if (recentOtp != null)
                return BadRequest("Vui lòng đợi 1 phút trước khi yêu cầu mã OTP mới.");

            // Xóa OTP cũ
            await CleanupOldOtpsAsync(email, purpose);

            // Tìm user nếu đã tồn tại (để set UserId)
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            // Tạo mã OTP mới
            var otpCode = GenerateOtp();
            var emailVerification = new EmailVerification
            {
                Email = email,
                OtpCode = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10), // OTP có hiệu lực 10 phút
                IsUsed = false,
                Purpose = purpose,
                UserId = existingUser?.Id // Set UserId nếu user đã tồn tại
            };

            _context.EmailVerifications.Add(emailVerification);
            await _context.SaveChangesAsync();

            // Gửi email
            var emailSent = await _emailService.SendOtpEmailAsync(email, otpCode, purpose);
            
            if (!emailSent)
            {
                // Nếu không gửi được email
                _logger.LogWarning($"⚠️ Failed to send OTP email to {email}, but OTP was saved: {otpCode}");
                
                // Trong môi trường development, trả về OTP trong response để test
                var isDevelopment = _config["ASPNETCORE_ENVIRONMENT"] == "Development" || 
                                   _config["Environment"] == "Development";
                
                if (isDevelopment)
                {
                    return Ok(new { 
                        message = "⚠️ Không thể gửi email. (Development mode - OTP: " + otpCode + ")",
                        warning = "Email service chưa được cấu hình. Vui lòng cấu hình Email settings trong appsettings.json",
                        otpCode = otpCode // Chỉ trả về trong development khi email fail
                    });
                }
                
                // Production: không trả về OTP, chỉ thông báo lỗi
                return StatusCode(500, new { 
                    message = "Không thể gửi email. Vui lòng thử lại sau.",
                    error = "Email service configuration error"
                });
            }

            // Email gửi thành công - không trả về OTP
            _logger.LogInformation($"✅ OTP email sent successfully to {email}");
            return Ok(new { message = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư." });
        }

        // 🔹 POST /api/auth/verify-otp - Xác thực mã OTP
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();
            var otpCode = dto.OtpCode.Trim();
            var purpose = dto.Purpose ?? "Register";

            // Tìm OTP hợp lệ
            var emailVerification = await _context.EmailVerifications
                .Where(e => e.Email == email && 
                           e.OtpCode == otpCode && 
                           e.Purpose == purpose &&
                           !e.IsUsed &&
                           e.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            if (emailVerification == null)
                return BadRequest("Mã OTP không hợp lệ hoặc đã hết hạn.");

            // Đánh dấu OTP đã sử dụng
            emailVerification.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xác thực OTP thành công.", verified = true });
        }

        // 🔹 POST /api/auth/register-with-otp - Đăng ký với xác thực OTP
        [HttpPost("register-with-otp")]
        public async Task<IActionResult> RegisterWithOtp([FromBody] RegisterWithOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();

            // Kiểm tra email đã tồn tại
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
                return Conflict("Email đã được sử dụng.");

            // Xác thực OTP
            var emailVerification = await _context.EmailVerifications
                .Where(e => e.Email == email && 
                           e.OtpCode == dto.OtpCode.Trim() && 
                           e.Purpose == "Register" &&
                           !e.IsUsed &&
                           e.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            if (emailVerification == null)
                return BadRequest("Mã OTP không hợp lệ hoặc đã hết hạn.");

            // Tạo user mới
            var user = new User
            {
                Name = dto.Name.Trim(),
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Customer",
                IsEmailVerified = true // Đã xác thực qua OTP
            };

            _context.Users.Add(user);
            
            // Đánh dấu OTP đã sử dụng
            emailVerification.IsUsed = true;
            
            await _context.SaveChangesAsync();

            // 🔹 Tự động tạo ví cho user mới đăng ký
            var wallet = new Wallet
            {
                UserId = user.Id,
                Balance = 0,
                Currency = "VND",
                CreatedAt = DateTime.UtcNow
            };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { id = user.Id }, new 
            { 
                user.Id, 
                user.Name, 
                user.Email, 
                user.Role,
                user.IsEmailVerified,
                message = "Đăng ký thành công. Email đã được xác thực."
            });
        }

        // 🔹 POST /api/auth/login-with-otp - Đăng nhập bằng OTP (không cần mật khẩu)
        [HttpPost("login-with-otp")]
        public async Task<IActionResult> LoginWithOtp([FromBody] LoginWithOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
            
            if (user == null)
                return Unauthorized("Email không tồn tại trong hệ thống.");

            // 🔹 Kiểm tra tài khoản có bị vô hiệu hóa không
            if (!user.IsActive)
                return Unauthorized("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");

            // Xác thực OTP
            var emailVerification = await _context.EmailVerifications
                .Where(e => e.Email == email && 
                           e.OtpCode == dto.OtpCode.Trim() && 
                           e.Purpose == "Login" &&
                           !e.IsUsed &&
                           e.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            if (emailVerification == null)
                return BadRequest("Mã OTP không hợp lệ hoặc đã hết hạn.");

            // Đánh dấu OTP đã sử dụng
            emailVerification.IsUsed = true;
            await _context.SaveChangesAsync();

            // Tạo JWT token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email.Trim().ToLower()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:TokenLifetimeMinutes"] ?? "60"));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new AuthResponseDto 
            { 
                Token = tokenString, 
                Expires = expires,
                Message = "Đăng nhập thành công bằng OTP."
            });
        }

        // 🔹 POST /api/auth/resend-verification-otp - Gửi lại OTP xác thực email cho user chưa verify
        [HttpPost("resend-verification-otp")]
        public async Task<IActionResult> ResendVerificationOtp([FromBody] ResendVerificationOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
            
            if (user == null)
                return NotFound("Email không tồn tại trong hệ thống.");

            if (user.IsEmailVerified)
                return BadRequest("Email này đã được xác thực rồi.");

            // Kiểm tra rate limiting
            var recentOtp = await _context.EmailVerifications
                .Where(e => e.Email == email && 
                           e.Purpose == "Register" && 
                           e.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
                .FirstOrDefaultAsync();

            if (recentOtp != null)
                return BadRequest("Vui lòng đợi 1 phút trước khi yêu cầu mã OTP mới.");

            // Xóa OTP cũ
            await CleanupOldOtpsAsync(email, "Register");

            // Tạo mã OTP mới
            var otpCode = GenerateOtp();
            var emailVerification = new EmailVerification
            {
                Email = email,
                OtpCode = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                Purpose = "Register",
                UserId = user.Id // User đã tồn tại trong trường hợp này
            };

            _context.EmailVerifications.Add(emailVerification);
            await _context.SaveChangesAsync();

            // Gửi email
            var emailSent = await _emailService.SendOtpEmailAsync(email, otpCode, "Register");
            
            if (!emailSent)
            {
                _logger.LogWarning($"⚠️ Failed to send verification OTP email to {email}, but OTP was saved: {otpCode}");
                
                var isDevelopment = _config["ASPNETCORE_ENVIRONMENT"] == "Development" || 
                                   _config["Environment"] == "Development";
                
                if (isDevelopment)
                {
                    return Ok(new { 
                        message = "⚠️ Không thể gửi email. (Development mode - OTP: " + otpCode + ")",
                        warning = "Email service chưa được cấu hình.",
                        otpCode = otpCode
                    });
                }
                
                return StatusCode(500, new { 
                    message = "Không thể gửi email. Vui lòng thử lại sau.",
                    error = "Email service configuration error"
                });
            }

            _logger.LogInformation($"✅ Verification OTP email sent successfully to {email}");
            return Ok(new { message = "Mã OTP xác thực email đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư." });
        }

        // 🔹 POST /api/auth/verify-email - Xác thực email cho user đã đăng ký nhưng chưa verify
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
            
            if (user == null)
                return NotFound("Email không tồn tại trong hệ thống.");

            if (user.IsEmailVerified)
                return BadRequest("Email này đã được xác thực rồi.");

            // Xác thực OTP
            var emailVerification = await _context.EmailVerifications
                .Where(e => e.Email == email && 
                           e.OtpCode == dto.OtpCode.Trim() && 
                           e.Purpose == "Register" &&
                           !e.IsUsed &&
                           e.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            if (emailVerification == null)
                return BadRequest("Mã OTP không hợp lệ hoặc đã hết hạn.");

            // Cập nhật user đã verify
            user.IsEmailVerified = true;
            emailVerification.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Xác thực email thành công. Bạn có thể đăng nhập ngay bây giờ.",
                isEmailVerified = true
            });
        }

        // 🔹 Helper: Map User to UserDto
        private static UserDto MapUserToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                EnterpriseId = user.EnterpriseId,
                Enterprise = user.Enterprise == null ? null : new EnterpriseDto
                {
                    Id = user.Enterprise.Id,
                    Name = user.Enterprise.Name,
                    Description = user.Enterprise.Description
                },
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                ShippingAddress = user.ShippingAddress,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                ProvinceId = user.ProvinceId,
                DistrictId = user.DistrictId,
                WardId = user.WardId,
                AddressDetail = user.AddressDetail
            };
        }

        // 🔹 POST /api/auth/google - Đăng nhập/Đăng ký bằng Google
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Google login - Invalid model state: {ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(dto.IdToken))
            {
                _logger.LogWarning("Google login - IdToken is null or empty");
                return BadRequest(new { message = "Google token không được để trống." });
            }

            try
            {
                _logger.LogInformation("Google login - Verifying token...");
                
                // Xác thực Google id_token
                var socialUserInfo = await _socialAuthService.VerifyGoogleTokenAsync(dto.IdToken);
                
                if (socialUserInfo == null)
                {
                    _logger.LogWarning("Google login - Token verification failed");
                    return Unauthorized(new { message = "Google token không hợp lệ hoặc đã hết hạn. Vui lòng thử lại." });
                }

                _logger.LogInformation($"Google login - Token verified for user: {socialUserInfo.Email}");

                // Tìm user theo GoogleId hoặc Email
                var user = await _context.Users
                    .Include(u => u.Enterprise)
                    .FirstOrDefaultAsync(u => 
                        (!string.IsNullOrEmpty(u.GoogleId) && u.GoogleId == socialUserInfo.ProviderId) ||
                        u.Email.ToLower() == socialUserInfo.Email.ToLower()
                    );

                if (user != null)
                {
                    // User đã tồn tại - cập nhật thông tin nếu cần
                    if (string.IsNullOrEmpty(user.GoogleId))
                    {
                        user.GoogleId = socialUserInfo.ProviderId;
                    }

                    // Cập nhật avatar nếu có và chưa có
                    if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(socialUserInfo.PictureUrl))
                    {
                        user.AvatarUrl = socialUserInfo.PictureUrl;
                    }

                    // Cập nhật tên nếu thay đổi
                    if (string.IsNullOrEmpty(user.Name) || user.Name != socialUserInfo.Name)
                    {
                        user.Name = socialUserInfo.Name;
                    }

                    // Đánh dấu email đã verified (vì Google đã xác thực)
                    user.IsEmailVerified = true;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Tạo user mới
                    user = new User
                    {
                        Name = socialUserInfo.Name,
                        Email = socialUserInfo.Email.ToLower(),
                        Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password vì không cần
                        Role = "Customer",
                        GoogleId = socialUserInfo.ProviderId,
                        AvatarUrl = socialUserInfo.PictureUrl,
                        IsEmailVerified = true, // Google đã xác thực email
                        IsActive = true
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Load Enterprise nếu có
                    if (user.EnterpriseId.HasValue)
                    {
                        await _context.Entry(user)
                            .Reference(u => u.Enterprise)
                            .LoadAsync();
                    }
                }

                // Kiểm tra tài khoản có bị vô hiệu hóa không
                if (!user.IsActive)
                    return Unauthorized("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");

                // Tạo JWT token
                var (token, expires) = GenerateJwtToken(user);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Expires = expires,
                    User = MapUserToDto(user),
                    Message = "Đăng nhập bằng Google thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý đăng nhập Google. Vui lòng thử lại sau." });
            }
        }

        // 🔹 POST /api/auth/facebook - Đăng nhập/Đăng ký bằng Facebook
        [HttpPost("facebook")]
        public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Facebook login - Invalid model state: {ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(dto.AccessToken))
            {
                _logger.LogWarning("Facebook login - AccessToken is null or empty");
                return BadRequest(new { message = "Facebook token không được để trống." });
            }

            try
            {
                _logger.LogInformation("Facebook login - Verifying token...");
                
                // Xác thực Facebook access_token
                var socialUserInfo = await _socialAuthService.VerifyFacebookTokenAsync(dto.AccessToken);
                
                if (socialUserInfo == null)
                {
                    _logger.LogWarning("Facebook login - Token verification failed");
                    return Unauthorized(new { message = "Facebook token không hợp lệ hoặc đã hết hạn. Vui lòng thử lại." });
                }

                _logger.LogInformation($"Facebook login - Token verified for user: {socialUserInfo.Email}");

                // Tìm user theo FacebookId hoặc Email
                var user = await _context.Users
                    .Include(u => u.Enterprise)
                    .FirstOrDefaultAsync(u => 
                        (!string.IsNullOrEmpty(u.FacebookId) && u.FacebookId == socialUserInfo.ProviderId) ||
                        u.Email.ToLower() == socialUserInfo.Email.ToLower()
                    );

                if (user != null)
                {
                    // User đã tồn tại - cập nhật thông tin nếu cần
                    if (string.IsNullOrEmpty(user.FacebookId))
                    {
                        user.FacebookId = socialUserInfo.ProviderId;
                    }

                    // Cập nhật avatar nếu có và chưa có
                    if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(socialUserInfo.PictureUrl))
                    {
                        user.AvatarUrl = socialUserInfo.PictureUrl;
                    }

                    // Cập nhật tên nếu thay đổi
                    if (string.IsNullOrEmpty(user.Name) || user.Name != socialUserInfo.Name)
                    {
                        user.Name = socialUserInfo.Name;
                    }

                    // Đánh dấu email đã verified dựa trên SocialUserInfo
                    user.IsEmailVerified = socialUserInfo.IsEmailVerified;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Tạo user mới
                    user = new User
                    {
                        Name = socialUserInfo.Name,
                        Email = socialUserInfo.Email.ToLower(),
                        Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password vì không cần
                        Role = "Customer",
                        FacebookId = socialUserInfo.ProviderId,
                        AvatarUrl = socialUserInfo.PictureUrl,
                        IsEmailVerified = socialUserInfo.IsEmailVerified, // Sử dụng giá trị từ SocialUserInfo
                        IsActive = true
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Load Enterprise nếu có
                    if (user.EnterpriseId.HasValue)
                    {
                        await _context.Entry(user)
                            .Reference(u => u.Enterprise)
                            .LoadAsync();
                    }
                }

                // Kiểm tra tài khoản có bị vô hiệu hóa không
                if (!user.IsActive)
                    return Unauthorized("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");

                // Tạo JWT token
                var (token, expires) = GenerateJwtToken(user);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Expires = expires,
                    User = MapUserToDto(user),
                    Message = "Đăng nhập bằng Facebook thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Facebook login");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý đăng nhập Facebook. Vui lòng thử lại sau." });
            }
        }

        // 🔹 POST /api/auth/forgot-password - Gửi OTP để đặt lại mật khẩu
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();

            // Kiểm tra email có tồn tại trong hệ thống không
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
            {
                // Không tiết lộ email có tồn tại hay không (security best practice)
                _logger.LogWarning($"ForgotPassword - Email not found: {email}");
                return Ok(new { message = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi mã OTP đến email của bạn." });
            }

            // Kiểm tra tài khoản có bị vô hiệu hóa không
            if (!user.IsActive)
            {
                _logger.LogWarning($"ForgotPassword - Account is inactive: {email}");
                return BadRequest("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
            }

            // Kiểm tra rate limiting: không cho gửi quá nhiều OTP trong 1 phút
            var recentOtp = await _context.EmailVerifications
                .Where(e => e.Email == email && 
                           e.Purpose == "ResetPassword" && 
                           e.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
                .FirstOrDefaultAsync();

            if (recentOtp != null)
                return BadRequest("Vui lòng đợi 1 phút trước khi yêu cầu mã OTP mới.");

            // Xóa OTP cũ
            await CleanupOldOtpsAsync(email, "ResetPassword");

            // Tạo mã OTP mới
            var otpCode = GenerateOtp();
            var emailVerification = new EmailVerification
            {
                Email = email,
                OtpCode = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10), // OTP có hiệu lực 10 phút
                IsUsed = false,
                Purpose = "ResetPassword",
                UserId = user.Id // User đã tồn tại trong trường hợp này
            };

            _context.EmailVerifications.Add(emailVerification);
            await _context.SaveChangesAsync();

            // Gửi email
            var emailSent = await _emailService.SendOtpEmailAsync(email, otpCode, "ResetPassword");
            
            if (!emailSent)
            {
                _logger.LogWarning($"⚠️ Failed to send reset password OTP email to {email}, but OTP was saved: {otpCode}");
                
                // Trong môi trường development, trả về OTP trong response để test
                var isDevelopment = _config["ASPNETCORE_ENVIRONMENT"] == "Development" || 
                                   _config["Environment"] == "Development";
                
                if (isDevelopment)
                {
                    return Ok(new { 
                        message = "⚠️ Không thể gửi email. (Development mode - OTP: " + otpCode + ")",
                        warning = "Email service chưa được cấu hình. Vui lòng cấu hình Email settings trong appsettings.json",
                        otpCode = otpCode // Chỉ trả về trong development khi email fail
                    });
                }
                
                // Production: không trả về OTP, chỉ thông báo lỗi
                return StatusCode(500, new { 
                    message = "Không thể gửi email. Vui lòng thử lại sau.",
                    error = "Email service configuration error"
                });
            }

            // Email gửi thành công
            _logger.LogInformation($"✅ Reset password OTP email sent successfully to {email}");
            return Ok(new { message = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi mã OTP đến email của bạn. Vui lòng kiểm tra hộp thư." });
        }

        // 🔹 POST /api/auth/reset-password - Đặt lại mật khẩu bằng OTP
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();
            var otpCode = dto.OtpCode.Trim();

            // Tìm user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
            {
                // Không tiết lộ email có tồn tại hay không
                return BadRequest("Mã OTP không hợp lệ hoặc đã hết hạn.");
            }

            // Kiểm tra tài khoản có bị vô hiệu hóa không
            if (!user.IsActive)
            {
                return BadRequest("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
            }

            // Xác thực OTP
            var emailVerification = await _context.EmailVerifications
                .Where(e => e.Email == email && 
                           e.OtpCode == otpCode && 
                           e.Purpose == "ResetPassword" &&
                           !e.IsUsed &&
                           e.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            if (emailVerification == null)
            {
                _logger.LogWarning($"ResetPassword - Invalid OTP for email: {email}");
                return BadRequest("Mã OTP không hợp lệ hoặc đã hết hạn.");
            }

            // Validate mật khẩu mới
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest(new { message = "Mật khẩu mới không được để trống" });
            }

            if (dto.NewPassword.Length < 6 || dto.NewPassword.Length > 100)
            {
                return BadRequest(new { message = "Mật khẩu mới phải có từ 6 đến 100 ký tự" });
            }

            // Kiểm tra format: phải có chữ hoa, chữ thường và số
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$"))
            {
                return BadRequest(new { message = "Mật khẩu mới phải chứa ít nhất một chữ hoa, một chữ thường và một số" });
            }

            // Kiểm tra mật khẩu xác nhận khớp
            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                return BadRequest(new { message = "Mật khẩu xác nhận không khớp với mật khẩu mới" });
            }

            // Kiểm tra mật khẩu mới phải khác mật khẩu hiện tại
            if (VerifyPassword(user, dto.NewPassword))
            {
                return BadRequest(new { message = "Mật khẩu mới phải khác mật khẩu hiện tại" });
            }

            // Hash mật khẩu mới
            user.Password = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.PasswordUpdatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Đánh dấu OTP đã sử dụng
            emailVerification.IsUsed = true;

            // Lưu vào database
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Password reset successfully for user: {email}");

            return Ok(new { 
                message = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập với mật khẩu mới.",
                success = true
            });
        }
    }
}
