using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using GiaLaiOCOP.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net;

namespace GiaLaiOCOP.Api.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary? _cloudinary;
        private readonly CloudinarySettings _settings;
        private readonly bool _isConfigured;

        public CloudinaryService(IOptions<CloudinarySettings> options)
        {
            _settings = options.Value ?? new CloudinarySettings();
            _isConfigured = !string.IsNullOrWhiteSpace(_settings.CloudName)
                            && !string.IsNullOrWhiteSpace(_settings.ApiKey)
                            && !string.IsNullOrWhiteSpace(_settings.ApiSecret);

            if (_isConfigured)
            {
                var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
                _cloudinary = new Cloudinary(account)
                {
                    Api = { Secure = true }
                };
            }
        }

        public async Task<CloudinaryUploadResult> UploadImageAsync(IFormFile file, string? folder = null, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Không có file hợp lệ để upload.", nameof(file));

            if (!_isConfigured || _cloudinary == null)
                throw new InvalidOperationException("Cloudinary chưa được cấu hình. Vui lòng bổ sung Cloudinary:CloudName, ApiKey, ApiSecret trong appsettings.");

            var targetFolder = string.IsNullOrWhiteSpace(folder) ? _settings.DefaultFolder : folder.Trim();

            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = targetFolder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            if (uploadResult == null || uploadResult.StatusCode != HttpStatusCode.OK)
            {
                var errorMessage = uploadResult?.Error?.Message ?? "Không thể upload ảnh lên Cloudinary.";
                throw new InvalidOperationException(errorMessage);
            }

            return new CloudinaryUploadResult
            {
                Url = uploadResult.SecureUrl?.ToString() ?? string.Empty,
                PublicId = uploadResult.PublicId ?? string.Empty,
                Width = uploadResult.Width,
                Height = uploadResult.Height,
                Format = uploadResult.Format
            };
        }
    }
}

