using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class CreateWalletRequestDto
    {
        [Required(ErrorMessage = "Loại yêu cầu là bắt buộc.")]
        [RegularExpression("^(deposit|withdraw)$", ErrorMessage = "Loại yêu cầu chỉ chấp nhận: deposit (nạp tiền), withdraw (rút tiền).")]
        public string Type { get; set; } = string.Empty; // deposit, withdraw

        [Required(ErrorMessage = "Số tiền là bắt buộc.")]
        [Range(1000, 100000000, ErrorMessage = "Số tiền phải từ 1,000 VND đến 100,000,000 VND.")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // ID tài khoản ngân hàng thụ hưởng (bắt buộc khi rút tiền)
        public int? BankAccountId { get; set; }
    }
}

