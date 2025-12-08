using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class UpdateWalletBalanceDto
    {
        [Required(ErrorMessage = "Số tiền là bắt buộc.")]
        public decimal Amount { get; set; } // Số tiền cộng/trừ (dương = cộng, âm = trừ)

        [Required(ErrorMessage = "Mô tả là bắt buộc.")]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty; // Lý do cập nhật số dư
    }
}

