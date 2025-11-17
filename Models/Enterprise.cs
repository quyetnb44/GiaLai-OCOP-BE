using System.Text.Json.Serialization;

namespace GiaLaiOCOP.Api.Models
{
    public class Enterprise
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // 🔹 Thông tin địa chỉ và tọa độ (cho Google Map)
        public string Address { get; set; } = string.Empty;            // Địa chỉ chi tiết
        public string Ward { get; set; } = string.Empty;               // Phường / Xã
        public string District { get; set; } = string.Empty;           // Quận / Huyện
        public string Province { get; set; } = string.Empty;           // Tỉnh / Thành phố
        public double? Latitude { get; set; }                          // Vĩ độ (cho marker trên map)
        public double? Longitude { get; set; }                         // Kinh độ (cho marker trên map)
        
        // 🔹 Thông tin liên hệ
        public string PhoneNumber { get; set; } = string.Empty;        // Số điện thoại
        public string EmailContact { get; set; } = string.Empty;       // Email liên hệ
        public string Website { get; set; } = string.Empty;            // Trang web
        
        // 🔹 Thông tin OCOP
        public int? OCOPRating { get; set; }                           // Xếp hạng OCOP (3-5 sao)
        public string BusinessField { get; set; } = string.Empty;      // Ngành hàng (Thực phẩm, đồ uống, thảo dược...)
        public string? ImageUrl { get; set; }                          // Ảnh đại diện doanh nghiệp
        
        // 🔹 Điểm đánh giá trung bình (tính từ Reviews của các sản phẩm)
        public double? AverageRating { get; set; }                     // Điểm đánh giá trung bình (1-5)
        
        // 🔹 Thông tin thanh toán (cho Bank Transfer)
        public string? BankCode { get; set; }                          // Mã ngân hàng (ví dụ: "970415" cho MB Bank)
        public string? BankAccount { get; set; }                       // Số tài khoản ngân hàng
        public string? BankAccountName { get; set; }                   // Tên chủ tài khoản
        
        // 🔹 Quan hệ
        // 1 doanh nghiệp có nhiều sản phẩm
        public ICollection<Product>? Products { get; set; }
        // 1 doanh nghiệp có nhiều người dùng (bao gồm admin & customer)
        [JsonIgnore] // 🔥 Ngăn vòng lặp khi serialize JSON
        public ICollection<User>? Users { get; set; }
        // 1 doanh nghiệp có nhiều payments
        [JsonIgnore]
        public ICollection<Payment>? Payments { get; set; }
        // 1 doanh nghiệp có nhiều ảnh
        [JsonIgnore]
        public ICollection<Image>? Images { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
