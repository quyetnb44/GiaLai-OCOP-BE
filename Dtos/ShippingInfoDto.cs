using System;

namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO cho thông tin vận chuyển trong chi tiết giao dịch
    /// </summary>
    public class ShippingInfoDto
    {
        public string? ShipperName { get; set; }        // Tên đơn vị giao hàng
        public string? TrackingNumber { get; set; }     // Mã vận đơn
        public string Status { get; set; } = string.Empty; // Pending, Shipped, Delivered
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveryNotes { get; set; }
        public string? ShippingAddress { get; set; }
    }
}
