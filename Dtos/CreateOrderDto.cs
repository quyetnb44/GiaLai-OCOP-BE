namespace GiaLaiOCOP.Api.Dtos
{
    public class CreateOrderDto
    {
        // 🔥 Xóa UserId vì lấy từ token
        
        // 🔹 Địa chỉ giao hàng: Có thể dùng ShippingAddressId (ưu tiên) hoặc ShippingAddress (string)
        // Nếu có ShippingAddressId, sẽ lấy địa chỉ từ bảng ShippingAddresses
        // Nếu không có ShippingAddressId, sẽ dùng ShippingAddress (string) - backward compatibility
        public int? ShippingAddressId { get; set; } // ID địa chỉ từ bảng ShippingAddresses
        public string? ShippingAddress { get; set; } // Địa chỉ string (backward compatibility)
        
        public List<OrderItemDto> Items { get; set; } = new();
        public string PaymentMethod { get; set; } = "COD"; // COD, BankTransfer
    }
}
