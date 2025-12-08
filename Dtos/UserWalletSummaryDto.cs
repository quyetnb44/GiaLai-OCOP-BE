namespace GiaLaiOCOP.Api.Dtos
{
    public class UserWalletSummaryDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public int WalletId { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime WalletCreatedAt { get; set; }
        public int TotalTransactions { get; set; } // Tổng số giao dịch
    }
}

