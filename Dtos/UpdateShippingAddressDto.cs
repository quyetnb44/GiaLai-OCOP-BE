using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class UpdateShippingAddressDto
    {
        [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc.")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        public string ShippingAddress { get; set; } = string.Empty;
    }
}

