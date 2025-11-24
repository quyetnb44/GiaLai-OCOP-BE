using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class InventoryHistory
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int EnterpriseId { get; set; }
        public Enterprise Enterprise { get; set; } = null!;
        public string Type { get; set; } = string.Empty; // import, export, adjustment
        public int Quantity { get; set; } // Change amount (can be negative)
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByUserId { get; set; }
        [JsonIgnore]
        public User? CreatedByUser { get; set; }
    }
}

