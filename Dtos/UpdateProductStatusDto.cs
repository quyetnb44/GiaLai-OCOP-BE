using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class UpdateProductStatusDto
    {
        [Required(ErrorMessage = "Status là bắt buộc.")]
        [RegularExpression("^(PendingApproval|Approved|Rejected)$", ErrorMessage = "Status chỉ chấp nhận: PendingApproval, Approved, Rejected.")]
        public string Status { get; set; } = "PendingApproval";

        [Range(3, 5, ErrorMessage = "OCOP rating phải từ 3 đến 5.")]
        public int? OCOPRating { get; set; }
    }
}

