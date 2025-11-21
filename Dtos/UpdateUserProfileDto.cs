using System;
using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class UpdateUserProfileDto
    {
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự.")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
        public string? PhoneNumber { get; set; }

        [StringLength(20, ErrorMessage = "Giới tính không được vượt quá 20 ký tự.")]
        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        public string? ShippingAddress { get; set; }

        [Url(ErrorMessage = "Avatar URL không hợp lệ.")]
        [StringLength(500, ErrorMessage = "Avatar URL không được vượt quá 500 ký tự.")]
        public string? AvatarUrl { get; set; }
    }
}

