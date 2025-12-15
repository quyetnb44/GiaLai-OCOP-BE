using System;

namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO hiển thị thông tin cơ bản của giao dịch trong danh sách
    /// </summary>
    public class TransactionHistoryDto
    {
        public string TransactionCode { get; set; } = string.Empty;
        public string? OrderCode { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "order", "wallet_deposit", "wallet_withdraw", "refund"
        public string? Description { get; set; }
    }
}
