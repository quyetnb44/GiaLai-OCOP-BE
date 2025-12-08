namespace GiaLaiOCOP.Api.Dtos
{
    public class WalletRequestDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public int WalletId { get; set; }
        public decimal CurrentBalance { get; set; }
        public string Type { get; set; } = string.Empty; // deposit, withdraw
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, approved, rejected, completed
        public string? RejectionReason { get; set; }
        public int? ProcessedBy { get; set; }
        public string? ProcessedByName { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Thông tin ngân hàng thụ hưởng (chỉ có khi rút tiền)
        public BankAccountInfoDto? BankAccount { get; set; }
    }

    public class BankAccountInfoDto
    {
        public int Id { get; set; }
        public string BankCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string? Branch { get; set; }
    }
}

