namespace GiaLaiOCOP.Api.Dtos
{
    public class CreateOrderDto
    {
        // 🔥 Xóa UserId vì lấy từ token
        public string ShippingAddress { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
        public string PaymentMethod { get; set; } = "COD"; // COD, BankTransfer
    }
}
