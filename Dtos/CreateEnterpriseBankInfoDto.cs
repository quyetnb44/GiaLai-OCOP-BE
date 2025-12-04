using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class CreateEnterpriseBankInfoDto
    {
        [Required(ErrorMessage = "Tên ngân hàng là bắt buộc.")]
        [MaxLength(255, ErrorMessage = "Tên ngân hàng không được vượt quá 255 ký tự.")]
        [Display(Name = "Tên ngân hàng")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số tài khoản là bắt buộc.")]
        [MaxLength(50, ErrorMessage = "Số tài khoản không được vượt quá 50 ký tự.")]
        [Display(Name = "Số tài khoản")]
        public string BankAccount { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên chủ tài khoản là bắt buộc.")]
        [MaxLength(255, ErrorMessage = "Tên chủ tài khoản không được vượt quá 255 ký tự.")]
        [Display(Name = "Chủ tài khoản")]
        public string AccountName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã ngân hàng là bắt buộc.")]
        [MaxLength(10, ErrorMessage = "Mã ngân hàng không được vượt quá 10 ký tự.")]
        [Display(Name = "Mã ngân hàng")]
        public string BankCode { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "Template không được vượt quá 20 ký tự.")]
        [Display(Name = "Template QR")]
        public string Template { get; set; } = "compact"; // "compact" hoặc "print"
    }
}

