namespace GiaLaiOCOP.Api.Models
{
    public class EmailVerification
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public string? Purpose { get; set; } // "Register", "Login", "ResetPassword"
    }
}

