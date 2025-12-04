namespace GiaLaiOCOP.Api.Dtos
{
    public class EnterpriseBankInfoDto
    {
        public int Id { get; set; }
        public int EnterpriseId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string Template { get; set; } = "compact";
        public string? QrCodeBase64 { get; set; } // QR code base64 (chỉ chứa thông tin tài khoản)
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // DTO cho response khi thanh toán (có thêm amount và description)
    public class PaymentQrCodeDto
    {
        public string QrCodeBase64 { get; set; } = string.Empty; // QR code với amount và description
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string EnterpriseBankName { get; set; } = string.Empty;
        public string EnterpriseAccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
    }
}

