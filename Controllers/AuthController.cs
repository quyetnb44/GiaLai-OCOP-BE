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

        public AuthController(AppDbContext context, IConfiguration config, IEmailService emailService, ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
            _logger = logger;
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

            // Tạo JWT token cho user mới đăng ký
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
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
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null) return Unauthorized("Email hoặc mật khẩu không đúng.");

            // 🔹 Kiểm tra password
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                return Unauthorized("Email hoặc mật khẩu không đúng.");

            // 🔹 Bỏ kiểm tra email verification - cho phép đăng nhập dù email chưa xác thực
            // Email verification chỉ là optional, không bắt buộc để đăng nhập

            // tạo claims
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
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

            // Tạo mã OTP mới
            var otpCode = GenerateOtp();
            var emailVerification = new EmailVerification
            {
                Email = email,
                OtpCode = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10), // OTP có hiệu lực 10 phút
                IsUsed = false,
                Purpose = purpose
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
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
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
                Purpose = "Register"
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
    }
}
