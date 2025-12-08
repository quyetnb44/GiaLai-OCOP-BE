namespace GiaLaiOCOP.Api.Dtos
{
    public class BankAccountDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string BankCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string? Branch { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? QrCodeUrl { get; set; } // URL QR code cho tài khoản này
    }
}

