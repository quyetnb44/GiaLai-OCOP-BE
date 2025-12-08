using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class WalletTransaction
    {
        public int Id { get; set; }

        [Required]
        public int WalletId { get; set; }

        [JsonIgnore]
        public Wallet? Wallet { get; set; }

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty; // deposit, withdraw, payment, refund

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Số dư sau giao dịch không được âm")]
        public decimal BalanceAfter { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "pending"; // pending, success, failed

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Thông tin liên quan đến đơn hàng (nếu có)
        public int? OrderId { get; set; }

        [JsonIgnore]
        public Order? Order { get; set; }

        // Thông tin giao dịch từ cổng thanh toán
        [MaxLength(255)]
        public string? PaymentGatewayTransactionId { get; set; }

        [MaxLength(50)]
        public string? PaymentGateway { get; set; } // momo, zalo, napas
    }
}

