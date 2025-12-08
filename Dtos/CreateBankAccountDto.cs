using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class CreateBankAccountDto
    {
        public int? UserId { get; set; } // SystemAdmin có thể tạo cho user khác, nếu null thì tạo cho chính mình

        [Required(ErrorMessage = "Mã ngân hàng là bắt buộc")]
        [StringLength(20, ErrorMessage = "Mã ngân hàng không được vượt quá 20 ký tự")]
        public string BankCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên ngân hàng là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên ngân hàng không được vượt quá 50 ký tự")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số tài khoản là bắt buộc")]
        [StringLength(50, ErrorMessage = "Số tài khoản không được vượt quá 50 ký tự")]
        public string AccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên chủ tài khoản là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên chủ tài khoản không được vượt quá 100 ký tự")]
        public string AccountName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Chi nhánh không được vượt quá 500 ký tự")]
        public string? Branch { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}

