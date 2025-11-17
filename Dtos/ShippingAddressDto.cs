using System;

namespace GiaLaiOCOP.Api.Dtos
{
    public class ShippingAddressDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? Label { get; set; }
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateShippingAddressDto
    {
        public string Address { get; set; } = string.Empty;
        public string? Label { get; set; }
        public bool IsDefault { get; set; } = false;
    }

    public class UpdateShippingAddressItemDto
    {
        public string? Address { get; set; }
        public string? Label { get; set; }
        public bool? IsDefault { get; set; }
    }
}

