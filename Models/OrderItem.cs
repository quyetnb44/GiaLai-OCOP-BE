using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [JsonIgnore] // 🔥 Ngăn vòng lặp khi serialize JSON
        public Order Order { get; set; }

        public int ProductId { get; set; }

        [JsonIgnore] // 🔥 Nếu Product có liên kết ngược đến OrderItem thì cũng nên bỏ qua
        public Product Product { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; } // Giá tại thời điểm đặt
    }
}
