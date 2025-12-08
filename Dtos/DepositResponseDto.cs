namespace GiaLaiOCOP.Api.Dtos
{
    public class DepositResponseDto
    {
        public string PaymentUrl { get; set; } = string.Empty; // QR Code URL
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentGateway { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty; // Mã tham chiếu để người dùng ghi chú khi chuyển khoản
    }
}

