namespace GiaLaiOCOP.Api.Services
{
    /// <summary>
    /// Service để xác thực token từ các nhà cung cấp OAuth (Google, Facebook)
    /// </summary>
    public interface ISocialAuthService
    {
        /// <summary>
        /// Xác thực Google id_token và lấy thông tin user
        /// </summary>
        Task<SocialUserInfo?> VerifyGoogleTokenAsync(string idToken);

        /// <summary>
        /// Xác thực Facebook access_token và lấy thông tin user
        /// </summary>
        Task<SocialUserInfo?> VerifyFacebookTokenAsync(string accessToken);
    }

    /// <summary>
    /// Thông tin user từ social provider
    /// </summary>
    public class SocialUserInfo
    {
        public string ProviderId { get; set; } = string.Empty; // Google ID hoặc Facebook ID
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? PictureUrl { get; set; } // URL ảnh đại diện
    }
}


