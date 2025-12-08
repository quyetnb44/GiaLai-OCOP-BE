using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class RefundRequestDto
    {
        [Required(ErrorMessage = "OrderId là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "OrderId phải lớn hơn 0.")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Số tiền hoàn là bắt buộc.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền hoàn phải lớn hơn 0.")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}

