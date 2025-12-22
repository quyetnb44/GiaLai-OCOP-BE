namespace GiaLaiOCOP.Api.Dtos
{
    public class OrderItemDto
    {
        public int Id { get; set; }              // Có thể cần khi update
        public int OrderId { get; set; }         // Liên kết đến Order
        public int ProductId { get; set; }       // Sản phẩm trong đơn
        public decimal Quantity { get; set; }        // Số lượng
        public decimal Price { get; set; }       // Giá từng sản phẩm
        public int? EnterpriseId { get; set; }  // EnterpriseId của sản phẩm (để frontend filter)
        public string? EnterpriseName { get; set; } // Tên enterprise (để frontend hiển thị)
        public string? EnterpriseImageUrl { get; set; } // URL ảnh enterprise (để frontend hiển thị logo)
        public string? ProductName { get; set; } // Tên sản phẩm (để frontend hiển thị)
        public string? ProductImageUrl { get; set; } // URL ảnh sản phẩm (để frontend hiển thị)
    }
}
