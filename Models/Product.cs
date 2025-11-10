using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        
        // 🔹 Thông tin OCOP và hình ảnh
        public string? ImageUrl { get; set; }                          // Ảnh sản phẩm
        public int? OCOPRating { get; set; }                           // Xếp hạng OCOP (3-5 sao)
        public string StockStatus { get; set; } = "InStock";           // Tình trạng: "InStock" (còn hàng) / "OutOfStock" (hết hàng)
        
        // 🔹 Điểm đánh giá trung bình (tính từ Reviews)
        public double? AverageRating { get; set; }                     // Điểm đánh giá trung bình (1-5)
        
        // Doanh nghiệp sở hữu sản phẩm
        public int EnterpriseId { get; set; }
        public Enterprise Enterprise { get; set; }

        [JsonIgnore] // 🧩 Bỏ danh sách OrderItems để tránh vòng lặp Product → OrderItem → Product
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
