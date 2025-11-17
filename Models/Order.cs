using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [JsonIgnore] // 🔥 Ngăn vòng lặp khi serialize JSON
        public User User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Completed, Cancelled

        // 🔹 Thông tin địa chỉ giao hàng (backward compatibility - giữ lại cho các đơn cũ)
        public string? ShippingAddress { get; set; }

        // 🔹 Liên kết đến ShippingAddress (địa chỉ đã lưu)
        public int? ShippingAddressId { get; set; } // Foreign key đến ShippingAddress
        public ShippingAddress? ShippingAddressDetail { get; set; } // Navigation property

        // 🔹 Thông tin giao hàng
        public int? ShipperId { get; set; } // Người giao hàng (Shipper)
        public DateTime? ShippedAt { get; set; } // Thời điểm giao hàng
        public DateTime? DeliveredAt { get; set; } // Thời điểm giao hàng thành công
        public string? DeliveryNotes { get; set; } // Ghi chú giao hàng

        // 🔹 Thông tin thanh toán
        public string PaymentMethod { get; set; } = "COD"; // COD, BankTransfer
        public string PaymentStatus { get; set; } = "Pending"; // Pending, AwaitingTransfer, Paid, PartiallyPaid, Cancelled
        public string? PaymentReference { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        [JsonIgnore]
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}