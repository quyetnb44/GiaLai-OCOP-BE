namespace GiaLaiOCOP.Api.Dtos
{
    public class OrderItemDto
    {
        public int Id { get; set; }              // Có thể cần khi update
        public int OrderId { get; set; }         // Liên kết đến Order
        public int ProductId { get; set; }       // Sản phẩm trong đơn
        public int Quantity { get; set; }        // Số lượng
        public decimal Price { get; set; }       // Giá từng sản phẩm
    }
}
