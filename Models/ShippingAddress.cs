using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Models
{
    public class ShippingAddress
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string AddressLine { get; set; } = string.Empty; // Số nhà, đường

        [Required]
        [StringLength(100)]
        public string Ward { get; set; } = string.Empty; // Phường/Xã

        [Required]
        [StringLength(100)]
        public string District { get; set; } = string.Empty; // Quận/Huyện

        [Required]
        [StringLength(100)]
        public string Province { get; set; } = string.Empty; // Tỉnh/Thành phố

        [Range(-90, 90, ErrorMessage = "Vĩ độ phải nằm trong khoảng -90 đến 90")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Kinh độ phải nằm trong khoảng -180 đến 180")]
        public double? Longitude { get; set; }

        [StringLength(50)]
        public string? Label { get; set; } // Ví dụ: "Nhà riêng", "Công ty"

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
