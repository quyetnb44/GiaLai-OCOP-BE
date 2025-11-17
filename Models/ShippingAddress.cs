using System;
using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    /// <summary>
    /// Model để lưu nhiều địa chỉ giao hàng cho mỗi Customer
    /// Mỗi User có thể có nhiều ShippingAddress
    /// </summary>
    public class ShippingAddress
    {
        public int Id { get; set; }

        // Foreign key đến User
        public int UserId { get; set; }

        // Địa chỉ giao hàng
        public string Address { get; set; } = string.Empty;

        // Nhãn địa chỉ (ví dụ: "Nhà riêng", "Công ty", "Địa chỉ 1")
        public string? Label { get; set; }

        // Địa chỉ mặc định (chỉ 1 địa chỉ mặc định cho mỗi user)
        public bool IsDefault { get; set; } = false;

        // Ngày tạo
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Ngày cập nhật
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [JsonIgnore] // Ngăn vòng lặp khi serialize JSON
        public User? User { get; set; }
    }
}

