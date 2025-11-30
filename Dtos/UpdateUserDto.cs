using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO để SystemAdmin cập nhật thông tin user (tất cả các trường)
    /// </summary>
    public class UpdateUserDto
    {
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên phải có từ 2 đến 100 ký tự.")]
        [Display(Name = "Tên")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [RegularExpression(@"^(SystemAdmin|EnterpriseAdmin|Customer)$",
            ErrorMessage = "Vai trò phải là SystemAdmin, EnterpriseAdmin hoặc Customer.")]
        [Display(Name = "Vai trò")]
        public string? Role { get; set; }

        [Display(Name = "Mã doanh nghiệp")]
        public int? EnterpriseId { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [StringLength(20, ErrorMessage = "Giới tính không được vượt quá 20 ký tự.")]
        [Display(Name = "Giới tính")]
        public string? Gender { get; set; }

        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string? ShippingAddress { get; set; }

        [Url(ErrorMessage = "Avatar URL không hợp lệ.")]
        [StringLength(500, ErrorMessage = "Avatar URL không được vượt quá 500 ký tự.")]
        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Trạng thái hoạt động")]
        public bool? IsActive { get; set; }

        [Display(Name = "Email đã xác thực")]
        public bool? IsEmailVerified { get; set; }

        // Địa chỉ chi tiết
        [Display(Name = "Mã tỉnh/thành phố")]
        public int? ProvinceId { get; set; }

        [Display(Name = "Mã quận/huyện")]
        public int? DistrictId { get; set; }

        [Display(Name = "Mã phường/xã")]
        public int? WardId { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ chi tiết không được vượt quá 500 ký tự.")]
        [Display(Name = "Địa chỉ chi tiết")]
        public string? AddressDetail { get; set; }
    }
}
