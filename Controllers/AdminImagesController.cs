using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;

namespace GiaLaiOCOP.Api.Controllers
{
    /// <summary>
    /// Controller quản lý ảnh cho SystemAdmin
    /// </summary>
    [Route("api/Admin")]
    [ApiController]
    [Authorize(Roles = "SystemAdmin")]
    public class AdminImagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminImagesController> _logger;

        public AdminImagesController(AppDbContext context, ILogger<AdminImagesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/Admin/Images - Xem tất cả ảnh trong hệ thống
        /// </summary>
        [HttpGet("Images")]
        public async Task<ActionResult<object>> GetAllImages(
            [FromQuery] string? imageType = null,
            [FromQuery] bool? isApproved = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Images.AsQueryable();

            // 🔹 Filter theo imageType
            if (!string.IsNullOrEmpty(imageType))
            {
                query = query.Where(img => img.ImageType == imageType);
            }

            // 🔹 Filter theo isApproved
            if (isApproved.HasValue)
            {
                query = query.Where(img => img.IsApproved == isApproved.Value);
            }

            // 🔹 Filter theo isActive
            if (isActive.HasValue)
            {
                query = query.Where(img => img.IsActive == isActive.Value);
            }

            var total = await query.CountAsync();

            var images = await query
                .Include(img => img.User)
                .Include(img => img.Product)
                    .ThenInclude(p => p!.Enterprise)
                .Include(img => img.Enterprise)
                .Include(img => img.UploadedByUser)
                .OrderByDescending(img => img.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(img => new
                {
                    id = img.Id,
                    url = img.Url,
                    fileName = img.FileName,
                    contentType = img.ContentType,
                    fileSize = img.FileSize,
                    imageType = img.ImageType,
                    userId = img.UserId,
                    productId = img.ProductId,
                    enterpriseId = img.EnterpriseId,
                    productName = img.Product != null ? img.Product.Name : null,
                    enterpriseName = img.Enterprise != null ? img.Enterprise.Name : null,
                    uploadedByUserId = img.UploadedByUserId,
                    uploadedByRole = img.UploadedByRole,
                    uploadedByName = img.UploadedByUser != null ? img.UploadedByUser.Name : null,
                    isActive = img.IsActive,
                    isApproved = img.IsApproved,
                    createdAt = img.CreatedAt,
                    updatedAt = img.UpdatedAt,
                    deletedAt = img.DeletedAt
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                images
            });
        }

        /// <summary>
        /// GET /api/Admin/Images/{imageId} - Xem chi tiết ảnh
        /// </summary>
        [HttpGet("Images/{imageId}")]
        public async Task<ActionResult<object>> GetImage(int imageId)
        {
            var image = await _context.Images
                .Include(img => img.User)
                .Include(img => img.Product)
                    .ThenInclude(p => p!.Enterprise)
                .Include(img => img.Enterprise)
                .Include(img => img.UploadedByUser)
                .FirstOrDefaultAsync(img => img.Id == imageId);

            if (image == null)
                return NotFound("Không tìm thấy ảnh.");

            return Ok(new
            {
                id = image.Id,
                url = image.Url,
                fileName = image.FileName,
                contentType = image.ContentType,
                fileSize = image.FileSize,
                imageType = image.ImageType,
                userId = image.UserId,
                productId = image.ProductId,
                enterpriseId = image.EnterpriseId,
                productName = image.Product != null ? image.Product.Name : null,
                enterpriseName = image.Enterprise != null ? image.Enterprise.Name : null,
                uploadedByUserId = image.UploadedByUserId,
                uploadedByRole = image.UploadedByRole,
                uploadedByName = image.UploadedByUser != null ? image.UploadedByUser.Name : null,
                isActive = image.IsActive,
                isApproved = image.IsApproved,
                width = image.Width,
                height = image.Height,
                createdAt = image.CreatedAt,
                updatedAt = image.UpdatedAt,
                deletedAt = image.DeletedAt
            });
        }

        /// <summary>
        /// PUT /api/Admin/Images/{imageId}/Approve - Duyệt ảnh
        /// </summary>
        [HttpPut("Images/{imageId}/Approve")]
        public async Task<IActionResult> ApproveImage(int imageId)
        {
            var image = await _context.Images.FindAsync(imageId);
            if (image == null)
                return NotFound("Không tìm thấy ảnh.");

            image.IsApproved = true;
            image.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã duyệt ảnh thành công." });
        }

        /// <summary>
        /// PUT /api/Admin/Images/{imageId}/Reject - Từ chối ảnh
        /// </summary>
        [HttpPut("Images/{imageId}/Reject")]
        public async Task<IActionResult> RejectImage(int imageId)
        {
            var image = await _context.Images.FindAsync(imageId);
            if (image == null)
                return NotFound("Không tìm thấy ảnh.");

            image.IsApproved = false;
            image.IsActive = false;
            image.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã từ chối ảnh." });
        }

        /// <summary>
        /// DELETE /api/Admin/Images/{imageId} - Xóa bất kỳ ảnh nào
        /// </summary>
        [HttpDelete("Images/{imageId}")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.Images.FindAsync(imageId);
            if (image == null)
                return NotFound("Không tìm thấy ảnh.");

            // 🔹 Soft delete
            image.IsActive = false;
            image.DeletedAt = DateTime.UtcNow;
            image.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xóa ảnh thành công." });
        }

        /// <summary>
        /// GET /api/Admin/Images/Stats - Thống kê ảnh
        /// </summary>
        [HttpGet("Images/Stats")]
        public async Task<ActionResult<object>> GetImageStats()
        {
            var totalImages = await _context.Images.CountAsync();
            var activeImages = await _context.Images.CountAsync(img => img.IsActive);
            var approvedImages = await _context.Images.CountAsync(img => img.IsApproved);
            var pendingImages = await _context.Images.CountAsync(img => !img.IsApproved && img.IsActive);

            var byType = await _context.Images
                .GroupBy(img => img.ImageType)
                .Select(g => new
                {
                    imageType = g.Key,
                    count = g.Count(),
                    activeCount = g.Count(img => img.IsActive),
                    approvedCount = g.Count(img => img.IsApproved)
                })
                .ToListAsync();

            return Ok(new
            {
                totalImages,
                activeImages,
                approvedImages,
                pendingImages,
                byType
            });
        }
    }
}

