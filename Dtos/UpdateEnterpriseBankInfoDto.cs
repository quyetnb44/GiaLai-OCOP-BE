using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class UpdateEnterpriseBankInfoDto
    {
        [MaxLength(255, ErrorMessage = "Tên ngân hàng không được vượt quá 255 ký tự.")]
        [Display(Name = "Tên ngân hàng")]
        public string? BankName { get; set; }

        [MaxLength(50, ErrorMessage = "Số tài khoản không được vượt quá 50 ký tự.")]
        [Display(Name = "Số tài khoản")]
        public string? BankAccount { get; set; }

        [MaxLength(255, ErrorMessage = "Tên chủ tài khoản không được vượt quá 255 ký tự.")]
        [Display(Name = "Chủ tài khoản")]
        public string? AccountName { get; set; }

        [MaxLength(10, ErrorMessage = "Mã ngân hàng không được vượt quá 10 ký tự.")]
        [Display(Name = "Mã ngân hàng")]
        public string? BankCode { get; set; }

        [MaxLength(20, ErrorMessage = "Template không được vượt quá 20 ký tự.")]
        [Display(Name = "Template QR")]
        public string? Template { get; set; }
    }
}

