using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Models
{
    /// <summary>
    /// Model để lưu thông tin ảnh trong hệ thống
    /// </summary>
    public class Image
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Url { get; set; } = string.Empty; // URL hoặc đường dẫn file

        [StringLength(255)]
        public string? FileName { get; set; } // Tên file gốc

        [StringLength(50)]
        public string? ContentType { get; set; } // image/jpeg, image/png, etc.

        public long? FileSize { get; set; } // Kích thước file (bytes)

        [Required]
        [StringLength(50)]
        public string ImageType { get; set; } = string.Empty; // "ProfileAvatar", "ProductImage", "EnterpriseImage", "Other"

        // 🔹 Tham chiếu đến resource sở hữu ảnh
        public int? UserId { get; set; } // Cho ảnh profile avatar
        public int? ProductId { get; set; } // Cho ảnh sản phẩm
        public int? EnterpriseId { get; set; } // Cho ảnh doanh nghiệp

        // 🔹 Thông tin người upload
        public int? UploadedByUserId { get; set; } // User ID của người upload (nullable để có thể set null khi xóa user)
        public string UploadedByRole { get; set; } = string.Empty; // Role của người upload

        // 🔹 Trạng thái
        public bool IsActive { get; set; } = true; // Ảnh có đang được sử dụng không
        public bool IsApproved { get; set; } = false; // Ảnh đã được SystemAdmin duyệt chưa (nếu cần)

        // 🔹 Thông tin metadata
        public int? Width { get; set; } // Chiều rộng ảnh (pixels)
        public int? Height { get; set; } // Chiều cao ảnh (pixels)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; } // Soft delete

        // Navigation properties
        public User? User { get; set; } // Cho ảnh profile
        public Product? Product { get; set; } // Cho ảnh sản phẩm
        public Enterprise? Enterprise { get; set; } // Cho ảnh doanh nghiệp
        public User? UploadedByUser { get; set; } // Người upload
    }
}

