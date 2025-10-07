using System;
using System.Collections.Generic;

namespace GiaLaiOCOP.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        // Thông tin cõ b?n
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Phân quy?n
        // "SystemAdmin" ? b?n (ngý?i s? h?u h? th?ng)
        // "EnterpriseAdmin" ? admin c?a doanh nghi?p
        // "Customer" ? khách hàng mua s?n ph?m
        public string Role { get; set; } = "Customer";

        // Ngày t?o tài kho?n
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Quan h?: 1 user có th? có nhi?u ðõn hàng
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        // ?? N?u là admin c?a doanh nghi?p th? thu?c v? m?t Enterprise
        public int? EnterpriseId { get; set; }
        public Enterprise? Enterprise { get; set; }
    }
}
