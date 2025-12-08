using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class DepositRequestDto
    {
        [Required(ErrorMessage = "Số tiền là bắt buộc.")]
        [Range(1000, 100000000, ErrorMessage = "Số tiền nạp phải từ 1,000 VND đến 100,000,000 VND.")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}

