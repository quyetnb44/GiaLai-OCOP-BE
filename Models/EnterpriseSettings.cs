namespace GiaLaiOCOP.Api.Models
{
    public class EnterpriseSettings
    {
        public int Id { get; set; }
        public int EnterpriseId { get; set; }
        public Enterprise Enterprise { get; set; } = null!;
        public string ShippingMethodsJson { get; set; } = "[]";
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactAddress { get; set; } = string.Empty;
        public string BusinessHours { get; set; } = "08:00 - 17:00";
        public string? ReturnPolicy { get; set; }
        public string? ShippingPolicy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

