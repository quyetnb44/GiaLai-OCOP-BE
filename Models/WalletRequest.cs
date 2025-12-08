namespace GiaLaiOCOP.Api.Models
{
    public class WalletRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int WalletId { get; set; }
        public string Type { get; set; } = string.Empty; // deposit, withdraw
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending, approved, rejected, completed
        public string? RejectionReason { get; set; }
        public int? ProcessedBy { get; set; } // SystemAdmin ID
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Thông tin ngân hàng thụ hưởng (chỉ dùng khi rút tiền)
        public int? BankAccountId { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public Wallet Wallet { get; set; } = null!;
        public User? ProcessedByUser { get; set; }
        public BankAccount? BankAccount { get; set; }
    }
}

