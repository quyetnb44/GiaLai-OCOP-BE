using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class EnterpriseBankInfo
    {
        public int Id { get; set; }
        
        public int EnterpriseId { get; set; }
        
        [JsonIgnore]
        public Enterprise? Enterprise { get; set; }
        
        // 🔹 Thông tin ngân hàng
        [MaxLength(255)]
        public string BankName { get; set; } = string.Empty; // Tên ngân hàng
        
        [MaxLength(50)]
        public string BankAccount { get; set; } = string.Empty; // Số tài khoản
        
        [MaxLength(255)]
        public string AccountName { get; set; } = string.Empty; // Chủ tài khoản
        
        [MaxLength(10)]
        public string BankCode { get; set; } = string.Empty; // Mã ngân hàng theo Napas (ví dụ: "970415")
        
        [MaxLength(20)]
        public string Template { get; set; } = "compact"; // Template QR: "compact" hoặc "print"
        
        // 🔹 QR Code đã được tạo sẵn (base64)
        public string? QrCodeBase64 { get; set; } // QR code base64 (chỉ chứa thông tin tài khoản, không có amount)
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

