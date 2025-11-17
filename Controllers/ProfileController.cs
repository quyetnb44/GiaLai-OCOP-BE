using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace GiaLaiOCOP.Api.Controllers
{
    /// <summary>
    /// Controller quản lý profile của Customer (avatar)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            AppDbContext context,
            IWebHostEnvironment environment,
            ILogger<ProfileController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        private async Task<int?> GetCurrentUserIdAsync()
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(claimValue))
                return null;

            if (int.TryParse(claimValue, out var userId))
                return userId;

            if (claimValue.Contains("@"))
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == claimValue);
                return user?.Id;
            }

            return null;
        }

        /// <summary>
        /// POST /api/Profile/Avatar - Upload avatar cho Customer
        /// </summary>
        [HttpPost("Avatar")]
        [RequestSizeLimit(5 * 1024 * 1024)] // 5MB
        public async Task<ActionResult<object>> UploadAvatar(IFormFile file)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            // 🔹 Validate file
            var validationResult = ValidateImageFile(file);
            if (validationResult != null)
                return validationResult;

            try
            {
                // 🔹 Upload file và lấy URL
                var uploadResult = await UploadImageFileAsync(file, "avatars");
                if (uploadResult.Error != null)
                    return BadRequest(uploadResult.Error);

                // 🔹 Lưu thông tin ảnh vào database
                var image = new Image
                {
                    Url = uploadResult.Url!,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    ImageType = "ProfileAvatar",
                    UserId = userId.Value,
                    UploadedByUserId = userId.Value,
                    UploadedByRole = "Customer",
                    IsActive = true,
                    IsApproved = true, // Customer tự upload avatar nên tự động approved
                    CreatedAt = DateTime.UtcNow
                };

                // 🔹 Vô hiệu hóa avatar cũ (nếu có)
                var oldAvatars = await _context.Images
                    .Where(img => img.UserId == userId.Value && 
                                 img.ImageType == "ProfileAvatar" && 
                                 img.IsActive)
                    .ToListAsync();

                foreach (var oldAvatar in oldAvatars)
                {
                    oldAvatar.IsActive = false;
                    oldAvatar.UpdatedAt = DateTime.UtcNow;
                }

                _context.Images.Add(image);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Upload avatar thành công.",
                    imageId = image.Id,
                    imageUrl = image.Url,
                    fileName = image.FileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload avatar cho user {UserId}", userId);
                return StatusCode(500, new { error = "Đã xảy ra lỗi khi upload avatar." });
            }
        }

        /// <summary>
        /// PUT /api/Profile/Avatar - Update avatar (thực chất là upload mới và vô hiệu hóa cũ)
        /// </summary>
        [HttpPut("Avatar")]
        [RequestSizeLimit(5 * 1024 * 1024)] // 5MB
        public async Task<ActionResult<object>> UpdateAvatar(IFormFile file)
        {
            // 🔹 Update avatar giống như upload mới
            return await UploadAvatar(file);
        }

        /// <summary>
        /// DELETE /api/Profile/Avatar - Xóa avatar (vô hiệu hóa)
        /// </summary>
        [HttpDelete("Avatar")]
        public async Task<IActionResult> DeleteAvatar()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var avatar = await _context.Images
                .Where(img => img.UserId == userId.Value && 
                             img.ImageType == "ProfileAvatar" && 
                             img.IsActive)
                .FirstOrDefaultAsync();

            if (avatar == null)
                return NotFound("Không tìm thấy avatar.");

            // 🔹 Soft delete
            avatar.IsActive = false;
            avatar.DeletedAt = DateTime.UtcNow;
            avatar.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xóa avatar thành công." });
        }

        /// <summary>
        /// GET /api/Profile/Avatar - Lấy avatar hiện tại
        /// </summary>
        [HttpGet("Avatar")]
        public async Task<ActionResult<object>> GetAvatar()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var avatar = await _context.Images
                .Where(img => img.UserId == userId.Value && 
                             img.ImageType == "ProfileAvatar" && 
                             img.IsActive)
                .OrderByDescending(img => img.CreatedAt)
                .FirstOrDefaultAsync();

            if (avatar == null)
                return Ok(new { imageUrl = (string?)null });

            return Ok(new
            {
                imageId = avatar.Id,
                imageUrl = avatar.Url,
                fileName = avatar.FileName,
                createdAt = avatar.CreatedAt
            });
        }

        // ============================================
        // 🔹 Helper Methods
        // ============================================

        private ActionResult? ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Không có file được tải lên." });

            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(new { error = "Chỉ chấp nhận file hình ảnh: JPG, JPEG, PNG." });

            // Kiểm tra kích thước (tối đa 5MB cho avatar)
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { error = "Kích thước file không được vượt quá 5MB." });

            return null;
        }

        private async Task<(string? Url, string? Error)> UploadImageFileAsync(IFormFile file, string subFolder)
        {
            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "images", subFolder);
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var imageUrl = $"{baseUrl}/uploads/images/{subFolder}/{fileName}";

                return (imageUrl, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload file {FileName}", file.FileName);
                return (null, "Đã xảy ra lỗi khi upload file.");
            }
        }
    }
}

