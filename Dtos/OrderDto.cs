using System;
using System.Collections.Generic;

namespace GiaLaiOCOP.Api.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }                     // Id của đơn hàng
        public int UserId { get; set; }                 // Người mua
        public DateTime OrderDate { get; set; }         // Ngày đặt
        public string? ShippingAddress { get; set; }    // Địa chỉ giao hàng
        public decimal TotalAmount { get; set; }        // Tổng tiền
        public string? Status { get; set; }             // Trạng thái

        // Danh sách chi tiết đơn hàng (có thể null nếu chỉ lấy đơn)
        public List<OrderItemDto>? OrderItems { get; set; }
    }
}
