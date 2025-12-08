namespace GiaLaiOCOP.Api.Dtos
{
    public class WalletTransactionDto
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public string Type { get; set; } = string.Empty; // deposit, withdraw, payment, refund
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty; // pending, success, failed
        public DateTime CreatedAt { get; set; }
        public int? OrderId { get; set; }
        public string? PaymentGatewayTransactionId { get; set; }
        public string? PaymentGateway { get; set; }
    }
}

