using System;
using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [JsonIgnore]
        public Order? Order { get; set; }

        public int EnterpriseId { get; set; }

        [JsonIgnore]
        public Enterprise? Enterprise { get; set; }

        public decimal Amount { get; set; }
        public string Method { get; set; } = "COD"; // COD, BankTransfer
        public string Status { get; set; } = "Pending"; // Pending, AwaitingTransfer, Paid, Cancelled
        public string Reference { get; set; } = string.Empty;

        // 🔹 Thông tin bank transfer (nếu có)
        public string? BankCode { get; set; }
        public string? BankAccount { get; set; }
        public string? AccountName { get; set; }
        public string? QrCodeUrl { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }
}

