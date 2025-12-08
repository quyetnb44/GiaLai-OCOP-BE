using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class UpdateBankAccountDto
    {
        [StringLength(20, ErrorMessage = "Mã ngân hàng không được vượt quá 20 ký tự")]
        public string? BankCode { get; set; }

        [StringLength(50, ErrorMessage = "Tên ngân hàng không được vượt quá 50 ký tự")]
        public string? BankName { get; set; }

        [StringLength(50, ErrorMessage = "Số tài khoản không được vượt quá 50 ký tự")]
        public string? AccountNumber { get; set; }

        [StringLength(100, ErrorMessage = "Tên chủ tài khoản không được vượt quá 100 ký tự")]
        public string? AccountName { get; set; }

        [StringLength(500, ErrorMessage = "Chi nhánh không được vượt quá 500 ký tự")]
        public string? Branch { get; set; }

        public bool? IsDefault { get; set; }

        public bool? IsActive { get; set; }
    }
}

