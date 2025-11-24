using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using GiaLaiOCOP.Api.Data;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FileUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadController> _logger;
        private readonly AppDbContext _context;

        public FileUploadController(IWebHostEnvironment environment, ILogger<FileUploadController> logger, AppDbContext context)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
        }

        private async Task<int?> GetUserIdFromTokenAsync()
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
        /// Upload hình ảnh (sản phẩm, doanh nghiệp, user avatar)
        /// </summary>
        [HttpPost("image")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
        public async Task<ActionResult<object>> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file được tải lên.");

            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("Chỉ chấp nhận file hình ảnh: JPG, JPEG, PNG, GIF, WEBP.");

            // Kiểm tra kích thước (tối đa 10MB)
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest("Kích thước file không được vượt quá 10MB.");

            try
            {
                // Tạo thư mục uploads/images nếu chưa có
                var uploadsFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Tạo tên file unique
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Trả về URL để frontend sử dụng
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var imageUrl = $"{baseUrl}/uploads/images/{fileName}";

                return Ok(new
                {
                    success = true,
                    message = "Upload hình ảnh thành công.",
                    imageUrl = imageUrl,
                    fileName = fileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload hình ảnh.");
                return StatusCode(500, "Đã xảy ra lỗi khi upload hình ảnh.");
            }
        }

        /// <summary>
        /// Upload nhiều hình ảnh cùng lúc
        /// </summary>
        [HttpPost("images")]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB tổng
        public async Task<ActionResult<object>> UploadMultipleImages(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("Không có file nào được tải lên.");

            if (files.Count > 10)
                return BadRequest("Chỉ có thể upload tối đa 10 hình ảnh cùng lúc.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var uploadedFiles = new List<object>();
            var errors = new List<string>();

            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    errors.Add($"File {file?.FileName} rỗng.");
                    continue;
                }

                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    errors.Add($"File {file.FileName} không đúng định dạng.");
                    continue;
                }

                if (file.Length > 10 * 1024 * 1024)
                {
                    errors.Add($"File {file.FileName} vượt quá 10MB.");
                    continue;
                }

                try
                {
                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    uploadedFiles.Add(new
                    {
                        fileName = file.FileName,
                        imageUrl = $"{baseUrl}/uploads/images/{fileName}",
                        size = file.Length
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi upload file {FileName}", file.FileName);
                    errors.Add($"Lỗi khi upload {file.FileName}.");
                }
            }

            return Ok(new
            {
                success = uploadedFiles.Count > 0,
                uploadedFiles = uploadedFiles,
                errors = errors,
                totalUploaded = uploadedFiles.Count,
                totalFailed = errors.Count
            });
        }

        /// <summary>
        /// Upload tài liệu xác thực (PDF hoặc ảnh) cho doanh nghiệp
        /// </summary>
        [HttpPost("document")]
        [Authorize(Roles = "EnterpriseAdmin")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
        public async Task<ActionResult<object>> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file được tải lên.");

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("Chỉ chấp nhận file PDF hoặc ảnh (JPG, JPEG, PNG).");

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user?.EnterpriseId == null)
                return Forbid("Bạn không thuộc doanh nghiệp nào.");

            try
            {
                var uploadsFolder = Path.Combine(
                    _environment.ContentRootPath,
                    "uploads",
                    "documents",
                    "enterprises",
                    user.EnterpriseId.Value.ToString());

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var documentUrl = $"{baseUrl}/uploads/documents/enterprises/{user.EnterpriseId}/{fileName}";

                return Ok(new
                {
                    success = true,
                    message = "Upload tài liệu thành công.",
                    documentUrl,
                    fileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload tài liệu.");
                return StatusCode(500, "Đã xảy ra lỗi khi upload tài liệu.");
            }
        }
    }
}

