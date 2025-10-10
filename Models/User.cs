using System;
using System.Collections.Generic;

namespace GiaLaiOCOP.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        // Th�ng tin c� b?n
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Ph�n quy?n
        // "SystemAdmin" ? b?n (ng�?i s? h?u h? th?ng)
        // "EnterpriseAdmin" ? admin c?a doanh nghi?p
        // "Customer" ? kh�ch h�ng mua s?n ph?m
        public string Role { get; set; } = "Customer";

        // Ng�y t?o t�i kho?n
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Quan h?: 1 user c� th? c� nhi?u ��n h�ng
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        // ?? N?u l� admin c?a doanh nghi?p th? thu?c v? m?t Enterprise
        public int? EnterpriseId { get; set; }
        public Enterprise? Enterprise { get; set; }
    }
}
