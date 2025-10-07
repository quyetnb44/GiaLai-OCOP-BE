namespace GiaLaiOCOP.Api.Dtos
{
    public class CreateOrderDto
    {
        public int UserId { get; set; }
        public string ShippingAddress { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
