using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // product_approved, product_rejected, new_order, low_stock, system
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Read { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Link { get; set; }

        public int? EnterpriseId { get; set; }
        [JsonIgnore]
        public Enterprise? Enterprise { get; set; }

        public int? UserId { get; set; }
        [JsonIgnore]
        public User? User { get; set; }

        public int? ProductId { get; set; }
        [JsonIgnore]
        public Product? Product { get; set; }

        public int? OrderId { get; set; }
        [JsonIgnore]
        public Order? Order { get; set; }
    }
}

