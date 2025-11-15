using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FileUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(IWebHostEnvironment environment, ILogger<FileUploadController> logger)
        {
            _environment = environment;
            _logger = logger;
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
    }
}

