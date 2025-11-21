using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        // Thông tin cơ bản
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ShippingAddress { get; set; }
        public string? AvatarUrl { get; set; }

        // Phân quyền
        // "SystemAdmin" → bạn (người sở hữu hệ thống)
        // "EnterpriseAdmin" → admin của doanh nghiệp
        // "Customer" → khách hàng mua sản phẩm
        public string Role { get; set; } = "Customer";

        // Ngày tạo tài khoản
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Xác thực email
        public bool IsEmailVerified { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }

        // Quan hệ: 1 user có thể có nhiều đơn hàng
        [JsonIgnore] // 🔥 Ngăn vòng lặp khi serialize JSON
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        // 💼 Nếu là admin của doanh nghiệp thì thuộc về một Enterprise
        public int? EnterpriseId { get; set; }
        public Enterprise? Enterprise { get; set; }

        // 🔹 Danh sách địa chỉ giao hàng đã lưu
        [JsonIgnore]
        public ICollection<ShippingAddress> ShippingAddresses { get; set; } = new List<ShippingAddress>();

        // 🔹 Ảnh đại diện (avatar)
        [JsonIgnore]
        public ICollection<Image> Images { get; set; } = new List<Image>();
    }
}
