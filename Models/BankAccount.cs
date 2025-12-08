using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class BankAccount
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        [Required]
        [StringLength(20)]
        public string BankCode { get; set; } = string.Empty; // Mã ngân hàng (ví dụ: 970422 cho MB Bank)

        [Required]
        [StringLength(50)]
        public string BankName { get; set; } = string.Empty; // Tên ngân hàng (ví dụ: MB Bank)

        [Required]
        [StringLength(50)]
        public string AccountNumber { get; set; } = string.Empty; // Số tài khoản

        [Required]
        [StringLength(100)]
        public string AccountName { get; set; } = string.Empty; // Tên chủ tài khoản

        [StringLength(500)]
        public string? Branch { get; set; } // Chi nhánh (optional)

        public bool IsDefault { get; set; } = false; // Tài khoản mặc định

        public bool IsActive { get; set; } = true; // Trạng thái hoạt động

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

