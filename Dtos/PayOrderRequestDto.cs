using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class PayOrderRequestDto
    {
        [Required(ErrorMessage = "OrderId là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "OrderId phải lớn hơn 0.")]
        public int OrderId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}

