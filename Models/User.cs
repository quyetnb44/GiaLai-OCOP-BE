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

        // Phân quyền
        // "SystemAdmin" → bạn (người sở hữu hệ thống)
        // "EnterpriseAdmin" → admin của doanh nghiệp
        // "Customer" → khách hàng mua sản phẩm
        public string Role { get; set; } = "Customer";

        // Ngày tạo tài khoản
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Xác thực email
        public bool IsEmailVerified { get; set; } = false;

        // Địa chỉ giao hàng chính
        public string? ShippingAddress { get; set; }

        // Quan hệ: 1 user có thể có nhiều đơn hàng
        [JsonIgnore] // 🔥 Ngăn vòng lặp khi serialize JSON
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        // 💼 Nếu là admin của doanh nghiệp thì thuộc về một Enterprise
        public int? EnterpriseId { get; set; }
        public Enterprise? Enterprise { get; set; }
    }
}
