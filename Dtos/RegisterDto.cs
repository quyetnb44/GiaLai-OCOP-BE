using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Tên là bắt buộc.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên phải có từ 2 đến 100 ký tự.")]
        [Display(Name = "Tên")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có từ 6 đến 100 ký tự.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$", 
            ErrorMessage = "Mật khẩu phải chứa ít nhất một chữ hoa, một chữ thường và một số.")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = "";
    }
}