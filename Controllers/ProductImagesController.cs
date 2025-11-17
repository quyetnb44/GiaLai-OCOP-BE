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
    /// Controller quản lý ảnh sản phẩm (chỉ EnterpriseAdmin)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "EnterpriseAdmin")]
    public class ProductImagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductImagesController> _logger;

        public ProductImagesController(
            AppDbContext context,
            IWebHostEnvironment environment,
            ILogger<ProductImagesController> logger)
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
        /// POST /api/ProductImages/{productId}/Images - Upload ảnh sản phẩm
        /// </summary>
        [HttpPost("Products/{productId}/Images")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
        public async Task<ActionResult<object>> UploadProductImage(int productId, IFormFile file)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            // 🔹 Kiểm tra quyền: Product phải thuộc về Enterprise của EnterpriseAdmin
            var user = await _context.Users
                .Include(u => u.Enterprise)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null || user.EnterpriseId == null)
                return Forbid("Bạn không thuộc về doanh nghiệp nào.");

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return NotFound("Không tìm thấy sản phẩm.");

            if (product.EnterpriseId != user.EnterpriseId.Value)
                return Forbid("Bạn chỉ có thể upload ảnh cho sản phẩm của doanh nghiệp mình.");

            // 🔹 Validate file
            var validationResult = ValidateImageFile(file);
            if (validationResult != null)
                return validationResult;

            try
            {
                // 🔹 Upload file và lấy URL
                var uploadResult = await UploadImageFileAsync(file, "products");
                if (uploadResult.Error != null)
                    return BadRequest(new { error = uploadResult.Error });

                // 🔹 Lưu thông tin ảnh vào database
                var image = new Image
                {
                    Url = uploadResult.Url!,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    ImageType = "ProductImage",
                    ProductId = productId,
                    UploadedByUserId = userId.Value,
                    UploadedByRole = "EnterpriseAdmin",
                    IsActive = true,
                    IsApproved = false, // Cần SystemAdmin duyệt
                    CreatedAt = DateTime.UtcNow
                };

                _context.Images.Add(image);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Upload ảnh sản phẩm thành công. Ảnh đang chờ duyệt.",
                    imageId = image.Id,
                    imageUrl = image.Url,
                    fileName = image.FileName,
                    isApproved = image.IsApproved
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload ảnh sản phẩm {ProductId}", productId);
                return StatusCode(500, new { error = "Đã xảy ra lỗi khi upload ảnh." });
            }
        }

        /// <summary>
        /// DELETE /api/ProductImages/Products/{productId}/Images/{imageId} - Xóa ảnh sản phẩm
        /// </summary>
        [HttpDelete("Products/{productId}/Images/{imageId}")]
        public async Task<IActionResult> DeleteProductImage(int productId, int imageId)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            // 🔹 Kiểm tra quyền: Product phải thuộc về Enterprise của EnterpriseAdmin
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null || user.EnterpriseId == null)
                return Forbid("Bạn không thuộc về doanh nghiệp nào.");

            var image = await _context.Images
                .Include(img => img.Product)
                .FirstOrDefaultAsync(img => img.Id == imageId && img.ProductId == productId);

            if (image == null)
                return NotFound("Không tìm thấy ảnh.");

            if (image.Product == null || image.Product.EnterpriseId != user.EnterpriseId.Value)
                return Forbid("Bạn chỉ có thể xóa ảnh của sản phẩm thuộc doanh nghiệp mình.");

            // 🔹 Soft delete
            image.IsActive = false;
            image.DeletedAt = DateTime.UtcNow;
            image.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xóa ảnh thành công." });
        }

        /// <summary>
        /// GET /api/ProductImages/Products/{productId}/Images - Lấy danh sách ảnh sản phẩm
        /// </summary>
        [HttpGet("Products/{productId}/Images")]
        public async Task<ActionResult<IEnumerable<object>>> GetProductImages(int productId)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            // 🔹 Kiểm tra quyền
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null || user.EnterpriseId == null)
                return Forbid("Bạn không thuộc về doanh nghiệp nào.");

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return NotFound("Không tìm thấy sản phẩm.");

            if (product.EnterpriseId != user.EnterpriseId.Value)
                return Forbid("Bạn chỉ có thể xem ảnh của sản phẩm thuộc doanh nghiệp mình.");

            var images = await _context.Images
                .Where(img => img.ProductId == productId && img.IsActive)
                .OrderByDescending(img => img.CreatedAt)
                .Select(img => new
                {
                    id = img.Id,
                    url = img.Url,
                    fileName = img.FileName,
                    isApproved = img.IsApproved,
                    createdAt = img.CreatedAt
                })
                .ToListAsync();

            return Ok(images);
        }

        // ============================================
        // 🔹 Helper Methods
        // ============================================

        private ActionResult? ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Không có file được tải lên." });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(new { error = "Chỉ chấp nhận file hình ảnh: JPG, JPEG, PNG." });

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { error = "Kích thước file không được vượt quá 10MB." });

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

