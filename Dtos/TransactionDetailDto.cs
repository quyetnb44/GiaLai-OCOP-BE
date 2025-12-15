using System;
using System.Collections.Generic;

namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO chi tiết hiển thị đầy đủ thông tin giao dịch
    /// </summary>
    public class TransactionDetailDto
    {
        // Thông tin chung
        public int Id { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        
        // Thông tin khách hàng
        public CustomerInfoDto? Customer { get; set; }
        
        // Chi tiết đơn hàng (nếu là order transaction)
        public List<OrderItemDetailDto>? OrderItems { get; set; }
        
        // Thông tin thanh toán
        public List<PaymentInfoDto>? Payments { get; set; }
        
        // Thông tin vận chuyển (nếu có)
        public ShippingInfoDto? ShippingInfo { get; set; }
    }

    /// <summary>
    /// DTO cho chi tiết sản phẩm trong đơn hàng
    /// </summary>
    public class OrderItemDetailDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Subtotal => Price * Quantity;
        public string? ProductImage { get; set; }
    }
}
