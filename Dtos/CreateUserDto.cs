using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO để SystemAdmin tạo user mới (bất kỳ loại nào)
    /// </summary>
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Tên là bắt buộc.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên phải có từ 2 đến 100 ký tự.")]
        [Display(Name = "Tên")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có từ 6 đến 100 ký tự.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$",
            ErrorMessage = "Mật khẩu phải chứa ít nhất một chữ hoa, một chữ thường và một số.")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vai trò là bắt buộc.")]
        [RegularExpression(@"^(SystemAdmin|EnterpriseAdmin|Customer)$",
            ErrorMessage = "Vai trò phải là SystemAdmin, EnterpriseAdmin hoặc Customer.")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; } = "Customer";

        // EnterpriseId chỉ bắt buộc nếu Role là EnterpriseAdmin
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

        [Display(Name = "Trạng thái hoạt động")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Email đã xác thực")]
        public bool IsEmailVerified { get; set; } = false;
    }
}
