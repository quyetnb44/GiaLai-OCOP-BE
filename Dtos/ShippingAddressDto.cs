using System;
using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class ShippingAddressDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        
        // Thông tin người nhận
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        
        // Địa chỉ chi tiết
        public string AddressLine { get; set; } = string.Empty; // Số nhà, đường
        public string Ward { get; set; } = string.Empty; // Phường/Xã
        public string District { get; set; } = string.Empty; // Quận/Huyện
        public string Province { get; set; } = string.Empty; // Tỉnh/Thành phố
        
        // Địa chỉ đầy đủ (backward compatibility)
        public string Address { get; set; } = string.Empty;
        
        public string? Label { get; set; }
        public bool IsDefault { get; set; } = false;
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
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Số điện thoại không hợp lệ (phải có 10-11 chữ số)")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ chi tiết không được để trống")]
        [StringLength(200, ErrorMessage = "Địa chỉ chi tiết không được vượt quá 200 ký tự")]
        public string AddressLine { get; set; } = string.Empty; // Số nhà, đường

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
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string? FullName { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Số điện thoại không hợp lệ (phải có 10-11 chữ số)")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ chi tiết không được vượt quá 200 ký tự")]
        public string? AddressLine { get; set; }

        [StringLength(100, ErrorMessage = "Phường/Xã không được vượt quá 100 ký tự")]
        public string? Ward { get; set; }

        [StringLength(100, ErrorMessage = "Quận/Huyện không được vượt quá 100 ký tự")]
        public string? District { get; set; }

        [StringLength(100, ErrorMessage = "Tỉnh/Thành phố không được vượt quá 100 ký tự")]
        public string? Province { get; set; }

        [StringLength(50, ErrorMessage = "Nhãn không được vượt quá 50 ký tự")]
        public string? Label { get; set; }
        
        public bool? IsDefault { get; set; }
    }
}

