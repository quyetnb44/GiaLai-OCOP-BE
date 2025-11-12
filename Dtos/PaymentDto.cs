namespace GiaLaiOCOP.Api.Dtos
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int EnterpriseId { get; set; }
        public string? EnterpriseName { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string? BankCode { get; set; }
        public string? BankAccount { get; set; }
        public string? AccountName { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}

