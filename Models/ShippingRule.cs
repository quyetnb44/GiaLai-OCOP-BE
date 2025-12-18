namespace GiaLaiOCOP.Api.Models
{
    /// <summary>
    /// Bảng cấu hình phí vận chuyển theo khu vực
    /// </summary>
    public class ShippingRule
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Loại vùng: SameProvince (cùng tỉnh), SameRegion (cùng miền), DifferentRegion (khác miền)
        /// </summary>
        public string ZoneType { get; set; } = string.Empty;
        
        /// <summary>
        /// Tên hiển thị (VD: "Cùng tỉnh", "Cùng miền", "Khác miền")
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// Phí vận chuyển (VND)
        /// </summary>
        public decimal ShippingFee { get; set; }
        
        /// <summary>
        /// Mô tả thêm (optional)
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Trạng thái hoạt động
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

