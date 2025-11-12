using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class UpdatePaymentStatusDto
    {
        [Required(ErrorMessage = "Status là bắt buộc.")]
        [RegularExpression("^(Paid|Cancelled)$", ErrorMessage = "Status chỉ chấp nhận: Paid hoặc Cancelled.")]
        public string Status { get; set; } = "Paid"; // Paid, Cancelled
        
        public string? Notes { get; set; }
    }
}

