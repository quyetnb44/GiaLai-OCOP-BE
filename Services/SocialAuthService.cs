using System.Text.Json;
using GiaLaiOCOP.Api.Services;
using Microsoft.Extensions.Logging;

namespace GiaLaiOCOP.Api.Services
{
    public class SocialAuthService : ISocialAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SocialAuthService> _logger;
        private readonly IConfiguration _configuration;

        public SocialAuthService(HttpClient httpClient, ILogger<SocialAuthService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Xác thực Google id_token bằng cách gọi Google's tokeninfo endpoint
        /// </summary>
        public async Task<SocialUserInfo?> VerifyGoogleTokenAsync(string idToken)
        {
            try
            {
                // Google tokeninfo endpoint
                var url = $"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}";
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Google token verification failed: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var tokenInfo = JsonSerializer.Deserialize<GoogleTokenInfo>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (tokenInfo == null || string.IsNullOrEmpty(tokenInfo.Sub) || string.IsNullOrEmpty(tokenInfo.Email))
                {
                    _logger.LogWarning("Google token info is invalid or missing required fields");
                    return null;
                }

                // Kiểm tra audience (client ID) nếu có cấu hình
                var googleClientId = _configuration["Google:ClientId"];
                if (!string.IsNullOrEmpty(googleClientId) && tokenInfo.Audience != googleClientId)
                {
                    _logger.LogWarning($"Google token audience mismatch. Expected: {googleClientId}, Got: {tokenInfo.Audience}");
                    return null;
                }

                return new SocialUserInfo
                {
                    ProviderId = tokenInfo.Sub,
                    Email = tokenInfo.Email,
                    Name = tokenInfo.Name ?? tokenInfo.Email.Split('@')[0],
                    PictureUrl = tokenInfo.Picture
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Google token");
                return null;
            }
        }

        /// <summary>
        /// Xác thực Facebook access_token bằng cách gọi Facebook Graph API
        /// </summary>
        public async Task<SocialUserInfo?> VerifyFacebookTokenAsync(string accessToken)
        {
            try
            {
                // Facebook Graph API endpoint để lấy thông tin user
                // fields=id,name,email,picture để lấy các thông tin cần thiết
                var url = $"https://graph.facebook.com/me?fields=id,name,email,picture&access_token={accessToken}";
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Facebook token verification failed: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<FacebookUserInfo>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (userInfo == null || string.IsNullOrEmpty(userInfo.Id) || string.IsNullOrEmpty(userInfo.Email))
                {
                    _logger.LogWarning("Facebook user info is invalid or missing required fields");
                    return null;
                }

                // Lấy URL ảnh từ picture object
                string? pictureUrl = null;
                if (userInfo.Picture?.Data?.Url != null)
                {
                    pictureUrl = userInfo.Picture.Data.Url;
                }

                return new SocialUserInfo
                {
                    ProviderId = userInfo.Id,
                    Email = userInfo.Email,
                    Name = userInfo.Name ?? userInfo.Email.Split('@')[0],
                    PictureUrl = pictureUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Facebook token");
                return null;
            }
        }

        // Helper classes để deserialize JSON response
        private class GoogleTokenInfo
        {
            public string? Sub { get; set; } // Google User ID
            public string? Email { get; set; }
            public string? Name { get; set; }
            public string? Picture { get; set; }
            public string? Audience { get; set; } // Client ID
        }

        private class FacebookUserInfo
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Email { get; set; }
            public FacebookPicture? Picture { get; set; }
        }

        private class FacebookPicture
        {
            public FacebookPictureData? Data { get; set; }
        }

        private class FacebookPictureData
        {
            public string? Url { get; set; }
        }
    }
}


