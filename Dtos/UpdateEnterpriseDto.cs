using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Dtos
{
    public class UpdateEnterpriseDto
    {
        [MaxLength(255)]
        public string? Name { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        // 🔹 Thông tin địa chỉ và tọa độ
        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Ward { get; set; }

        [MaxLength(100)]
        public string? District { get; set; }

        [MaxLength(100)]
        public string? Province { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude phải nằm trong khoảng -90 đến 90.")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude phải nằm trong khoảng -180 đến 180.")]
        public double? Longitude { get; set; }

        // 🔹 Thông tin liên hệ
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [MaxLength(255)]
        public string? EmailContact { get; set; }

        [MaxLength(255)]
        public string? Website { get; set; }

        // 🔹 Thông tin OCOP
        [MaxLength(100)]
        public string? BusinessField { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        // 🔹 Các trường chỉ SystemAdmin mới có thể cập nhật
        [Range(0, 5, ErrorMessage = "OCOPRating phải nằm trong khoảng 0 đến 5.")]
        public int? OCOPRating { get; set; }

        [MaxLength(50)]
        public string? ApprovalStatus { get; set; }

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }
    }
}

