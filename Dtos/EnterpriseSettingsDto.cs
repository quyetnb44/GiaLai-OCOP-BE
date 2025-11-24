using System;
using System.Collections.Generic;

namespace GiaLaiOCOP.Api.Dtos
{
    public class EnterpriseSettingsDto
    {
        public int EnterpriseId { get; set; }
        public List<ShippingMethodDto> ShippingMethods { get; set; } = new();
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactAddress { get; set; } = string.Empty;
        public string BusinessHours { get; set; } = "08:00 - 17:00";
        public string? ReturnPolicy { get; set; }
        public string? ShippingPolicy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ShippingMethodDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public decimal Fee { get; set; }
    }
}

