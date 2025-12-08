using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class WithdrawRequestDto
    {
        [Required(ErrorMessage = "Số tiền rút là bắt buộc.")]
        [Range(10000, 100000000, ErrorMessage = "Số tiền rút phải từ 10,000 VND đến 100,000,000 VND.")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}

