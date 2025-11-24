using Microsoft.AspNetCore.Http;

namespace GiaLaiOCOP.Api.Services
{
    public interface ICloudinaryService
    {
        Task<CloudinaryUploadResult> UploadImageAsync(IFormFile file, string? folder = null, CancellationToken cancellationToken = default);
    }

    public class CloudinaryUploadResult
    {
        public string Url { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public string? Format { get; set; }
    }
}

