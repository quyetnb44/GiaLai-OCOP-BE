using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        // Doanh nghiệp sở hữu sản phẩm
        public int EnterpriseId { get; set; }
        public Enterprise Enterprise { get; set; }

        [JsonIgnore] // 🧩 Bỏ danh sách OrderItems để tránh vòng lặp Product → OrderItem → Product
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
