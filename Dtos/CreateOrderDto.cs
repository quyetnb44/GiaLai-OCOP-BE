namespace GiaLaiOCOP.Api.Dtos
{
    public class CreateOrderDto
    {
        // 🔥 Xóa UserId vì lấy từ token
        
        // 🔹 Địa chỉ giao hàng (optional - dùng nếu không có ShippingAddressId)
        public string? ShippingAddress { get; set; }
        
        // 🔹 ID của địa chỉ đã lưu trong ShippingAddresses (optional - ưu tiên hơn ShippingAddress)
        public int? ShippingAddressId { get; set; }
        
        public List<OrderItemDto> Items { get; set; } = new();
        public string PaymentMethod { get; set; } = "COD"; // COD, BankTransfer
    }
}
