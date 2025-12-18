using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [JsonIgnore] // 🔥 Ngăn vòng lặp khi serialize JSON
        public User User { get; set; } = null!;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Completed, Cancelled, PendingCompletion

        // 🔹 Thông tin xác nhận hoàn thành đơn hàng
        public DateTime? CompletionRequestedAt { get; set; } // Thời gian EnterpriseAdmin yêu cầu xác nhận hoàn thành
        public DateTime? CompletionApprovedAt { get; set; } // Thời gian SystemAdmin xác nhận hoàn thành
        public DateTime? CompletionRejectedAt { get; set; } // Thời gian SystemAdmin từ chối
        public string? CompletionRejectionReason { get; set; } // Lý do từ chối

        public string? ShippingAddress { get; set; } // Backward compatibility - địa chỉ string cũ

        // 🔹 Thông tin địa chỉ giao hàng từ bảng ShippingAddresses
        public int? ShippingAddressId { get; set; }
        [JsonIgnore]
        public ShippingAddress? ShippingAddressDetail { get; set; }

        // 🔹 Thông tin giao hàng
        public int? ShipperId { get; set; } // Người giao hàng (Shipper)
        public DateTime? ShippedAt { get; set; } // Thời điểm giao hàng
        public DateTime? DeliveredAt { get; set; } // Thời điểm giao hàng thành công
        public string? DeliveryNotes { get; set; } // Ghi chú giao hàng

        // 🔹 Thông tin thanh toán
        public string PaymentMethod { get; set; } = "COD"; // COD, BankTransfer
        public string PaymentStatus { get; set; } = "Pending"; // Pending, AwaitingTransfer, BankTransferConfirmed, BankTransferRejected, Paid, PartiallyPaid, Cancelled
        public string? PaymentReference { get; set; }
        public string? BankTransferRejectionReason { get; set; } // Lý do từ chối chuyển khoản

        // 🔹 Phí vận chuyển
        public decimal ShippingFee { get; set; } = 0; // Phí ship (VND)
        public string? ShippingZoneType { get; set; } // SameProvince, SameRegion, DifferentRegion

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        [JsonIgnore]
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}