using System;
using System.Collections.Generic;

namespace GiaLaiOCOP.Api.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }                     // Id của đơn hàng
        public int UserId { get; set; }                 // Người mua
        public DateTime OrderDate { get; set; }         // Ngày đặt
        public string? ShippingAddress { get; set; }    // Địa chỉ giao hàng (từ ShippingAddressDetail hoặc ShippingAddress string)
        public int? ShippingAddressId { get; set; }      // ID địa chỉ từ bảng ShippingAddresses (nếu có)
        public decimal TotalAmount { get; set; }        // Tổng tiền
        public string? Status { get; set; }             // Trạng thái
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public string? BankTransferRejectionReason { get; set; } // Lý do từ chối chuyển khoản

        // 🔹 Phí vận chuyển
        public decimal ShippingFee { get; set; } // Phí ship (VND)
        public string? ShippingZoneType { get; set; } // SameProvince, SameRegion, DifferentRegion

        // 🔹 Thông tin giao hàng
        public int? ShipperId { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveryNotes { get; set; }

        // 🔹 Thông tin xác nhận hoàn thành đơn hàng
        public DateTime? CompletionRequestedAt { get; set; }
        public DateTime? CompletionApprovedAt { get; set; }
        public DateTime? CompletionRejectedAt { get; set; }
        public string? CompletionRejectionReason { get; set; }

        // 🔹 Thông tin Customer (để EnterpriseAdmin xem thông tin người đặt hàng)
        public CustomerInfoDto? Customer { get; set; }

        // Danh sách chi tiết đơn hàng (có thể null nếu chỉ lấy đơn)
        public List<OrderItemDto>? OrderItems { get; set; }

        public List<PaymentDto>? Payments { get; set; }

        // 🔹 Trạng thái riêng của từng Enterprise trong đơn hàng (để SystemAdmin xem)
        public List<OrderEnterpriseStatusDto>? EnterpriseStatuses { get; set; }
    }

    // 🔹 DTO cho trạng thái riêng của từng Enterprise
    public class OrderEnterpriseStatusDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int EnterpriseId { get; set; }
        public string? EnterpriseName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public string? Notes { get; set; }
    }

    // 🔹 Thông tin Customer cơ bản (để tránh circular reference)
    public class CustomerInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Address { get; set; } // Địa chỉ đầy đủ của customer
    }
}
