using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class ProcessWalletRequestDto
    {
        [Required(ErrorMessage = "Hành động là bắt buộc.")]
        [RegularExpression("^(approve|reject)$", ErrorMessage = "Hành động chỉ chấp nhận: approve (phê duyệt), reject (từ chối).")]
        public string Action { get; set; } = string.Empty; // approve, reject

        [MaxLength(500)]
        public string? RejectionReason { get; set; } // Bắt buộc nếu action = reject
    }
}

