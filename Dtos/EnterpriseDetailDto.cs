namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO cho popup chi tiết doanh nghiệp khi click marker
    /// </summary>
    public class EnterpriseDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string EmailContact { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public double? AverageRating { get; set; }
        public int? OCOPRating { get; set; }
        public string BusinessField { get; set; } = string.Empty;
        
        // 🔹 3 sản phẩm nổi bật
        public List<ProductMapDto> FeaturedProducts { get; set; } = new List<ProductMapDto>();
        
        // 🔹 Tổng số sản phẩm
        public int TotalProducts { get; set; }
        
        // 🔹 Số lượng đánh giá
        public int RatingCount { get; set; }
        
        // 🔹 URL để mở Google Maps chỉ đường
        public string? DirectionsUrl { get; set; }
        
        // 🔹 Khoảng cách từ vị trí người dùng (km) - chỉ có khi có tọa độ người dùng
        public double? Distance { get; set; }
    }
}

