using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class ShippingAddressDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string? Label { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateShippingAddressDto
    {
        [Required(ErrorMessage = "Họ tên người nhận không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ chi tiết không được để trống")]
        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        public string AddressLine { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phường/Xã không được để trống")]
        [StringLength(100, ErrorMessage = "Phường/Xã không được vượt quá 100 ký tự")]
        public string Ward { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quận/Huyện không được để trống")]
        [StringLength(100, ErrorMessage = "Quận/Huyện không được vượt quá 100 ký tự")]
        public string District { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tỉnh/Thành phố không được để trống")]
        [StringLength(100, ErrorMessage = "Tỉnh/Thành phố không được vượt quá 100 ký tự")]
        public string Province { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Nhãn không được vượt quá 50 ký tự")]
        public string? Label { get; set; }

        public bool IsDefault { get; set; } = false;
    }

    public class UpdateShippingAddressItemDto
    {
        [Required(ErrorMessage = "Họ tên người nhận không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ chi tiết không được để trống")]
        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        public string AddressLine { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phường/Xã không được để trống")]
        [StringLength(100, ErrorMessage = "Phường/Xã không được vượt quá 100 ký tự")]
        public string Ward { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quận/Huyện không được để trống")]
        [StringLength(100, ErrorMessage = "Quận/Huyện không được vượt quá 100 ký tự")]
        public string District { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tỉnh/Thành phố không được để trống")]
        [StringLength(100, ErrorMessage = "Tỉnh/Thành phố không được vượt quá 100 ký tự")]
        public string Province { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Nhãn không được vượt quá 50 ký tự")]
        public string? Label { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}
