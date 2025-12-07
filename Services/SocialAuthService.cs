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
                if (string.IsNullOrWhiteSpace(idToken))
                {
                    _logger.LogWarning("Google idToken is null or empty");
                    return null;
                }

                // Google tokeninfo endpoint
                var url = $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}";
                
                _logger.LogInformation($"Verifying Google token with URL: {url.Substring(0, Math.Min(100, url.Length))}...");
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Google token verification failed: {response.StatusCode} - {errorContent}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Google tokeninfo response received: {content.Substring(0, Math.Min(200, content.Length))}...");
                
                var tokenInfo = JsonSerializer.Deserialize<GoogleTokenInfo>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (tokenInfo == null)
                {
                    _logger.LogWarning("Failed to deserialize Google token info");
                    return null;
                }

                if (string.IsNullOrEmpty(tokenInfo.Sub))
                {
                    _logger.LogWarning("Google token info missing Sub (user ID)");
                    return null;
                }

                if (string.IsNullOrEmpty(tokenInfo.Email))
                {
                    _logger.LogWarning("Google token info missing Email");
                    return null;
                }

                // Kiểm tra audience (client ID) nếu có cấu hình
                var googleClientId = _configuration["Google:ClientId"];
                if (!string.IsNullOrEmpty(googleClientId))
                {
                    // Remove any prefix if present (e.g., "NEXT_PUBLIC_GOOGLE_CLIENT_ID=")
                    googleClientId = googleClientId.Replace("NEXT_PUBLIC_GOOGLE_CLIENT_ID=", "").Trim();
                    
                    if (tokenInfo.Audience != googleClientId)
                    {
                        _logger.LogWarning($"Google token audience mismatch. Expected: {googleClientId}, Got: {tokenInfo.Audience}");
                        return null;
                    }
                    _logger.LogInformation($"Google token audience verified: {tokenInfo.Audience}");
                }

                var userInfo = new SocialUserInfo
                {
                    ProviderId = tokenInfo.Sub,
                    Email = tokenInfo.Email.ToLower().Trim(),
                    Name = tokenInfo.Name ?? tokenInfo.Email.Split('@')[0],
                    PictureUrl = tokenInfo.Picture
                };

                _logger.LogInformation($"Google token verified successfully for user: {userInfo.Email}");
                return userInfo;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error verifying Google token");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error verifying Google token");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error verifying Google token");
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
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    _logger.LogWarning("Facebook accessToken is null or empty");
                    return null;
                }

                // Facebook Graph API endpoint để lấy thông tin user
                // fields=id,name,email,picture để lấy các thông tin cần thiết
                var url = $"https://graph.facebook.com/me?fields=id,name,email,picture&access_token={Uri.EscapeDataString(accessToken)}";
                
                _logger.LogInformation($"Verifying Facebook token with URL: {url.Substring(0, Math.Min(100, url.Length))}...");
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Facebook token verification failed: {response.StatusCode} - {errorContent}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Facebook Graph API response received: {content.Substring(0, Math.Min(200, content.Length))}...");
                
                var userInfo = JsonSerializer.Deserialize<FacebookUserInfo>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (userInfo == null)
                {
                    _logger.LogWarning("Failed to deserialize Facebook user info");
                    return null;
                }

                if (string.IsNullOrEmpty(userInfo.Id))
                {
                    _logger.LogWarning("Facebook user info missing Id");
                    return null;
                }

                // Facebook có thể không trả về email nếu user không cấp quyền
                // Trong trường hợp này, ta sẽ tạo email tạm từ Facebook ID
                string email;
                if (string.IsNullOrEmpty(userInfo.Email))
                {
                    _logger.LogWarning($"Facebook user info missing Email for ID: {userInfo.Id}. Creating temporary email.");
                    // Tạo email tạm từ Facebook ID (sẽ yêu cầu user cập nhật sau)
                    email = $"fb_{userInfo.Id}@facebook.temp";
                }
                else
                {
                    email = userInfo.Email.ToLower().Trim();
                }

                // Lấy URL ảnh từ picture object
                string? pictureUrl = null;
                if (userInfo.Picture?.Data?.Url != null)
                {
                    pictureUrl = userInfo.Picture.Data.Url;
                }

                var socialUserInfo = new SocialUserInfo
                {
                    ProviderId = userInfo.Id,
                    Email = email,
                    Name = userInfo.Name ?? (userInfo.Email?.Split('@')[0] ?? $"User_{userInfo.Id}"),
                    PictureUrl = pictureUrl
                };

                _logger.LogInformation($"Facebook token verified successfully for user: {socialUserInfo.Email}");
                return socialUserInfo;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error verifying Facebook token");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error verifying Facebook token");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error verifying Facebook token");
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


