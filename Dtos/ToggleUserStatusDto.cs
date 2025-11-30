using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO để SystemAdmin vô hiệu hóa/kích hoạt tài khoản
    /// </summary>
    public class ToggleUserStatusDto
    {
        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        [Display(Name = "Trạng thái hoạt động")]
        public bool IsActive { get; set; }
    }
}
