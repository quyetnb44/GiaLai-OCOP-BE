using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    /// <summary>
    /// Model để lưu nhiều địa chỉ giao hàng cho mỗi Customer (giống Shopee)
    /// Mỗi User có thể có nhiều ShippingAddress
    /// </summary>
    public class ShippingAddress
    {
        public int Id { get; set; }

        // Foreign key đến User
        [Required]
        public int UserId { get; set; }

        // 🔹 Thông tin người nhận
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty; // Họ tên người nhận

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty; // Số điện thoại

        // 🔹 Địa chỉ chi tiết (theo cấu trúc Việt Nam)
        [Required]
        [StringLength(200)]
        public string AddressLine { get; set; } = string.Empty; // Số nhà, đường

        [Required]
        [StringLength(100)]
        public string Ward { get; set; } = string.Empty; // Phường/Xã

        [Required]
        [StringLength(100)]
        public string District { get; set; } = string.Empty; // Quận/Huyện

        [Required]
        [StringLength(100)]
        public string Province { get; set; } = string.Empty; // Tỉnh/Thành phố

        // Địa chỉ đầy đủ (tự động tạo từ các trường trên - để backward compatibility)
        public string Address { get; set; } = string.Empty; // Full address string

        // Nhãn địa chỉ (ví dụ: "Nhà riêng", "Công ty", "Địa chỉ 1")
        [StringLength(50)]
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

        // Navigation property đến Orders sử dụng địa chỉ này
        [JsonIgnore]
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}

