using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class UpdateShippingAddressDetailDto
    {
        [Required(ErrorMessage = "ProvinceId là bắt buộc.")]
        public int ProvinceId { get; set; }

        [Required(ErrorMessage = "DistrictId là bắt buộc.")]
        public int DistrictId { get; set; }

        [Required(ErrorMessage = "WardId là bắt buộc.")]
        public int WardId { get; set; }

        [Required(ErrorMessage = "Địa chỉ cụ thể là bắt buộc.")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        public string AddressDetail { get; set; } = string.Empty;
    }
}
















