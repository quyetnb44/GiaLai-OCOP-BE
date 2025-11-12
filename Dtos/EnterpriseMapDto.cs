namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO cho marker trên bản đồ (thông tin cơ bản)
    /// </summary>
    public class EnterpriseMapDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ImageUrl { get; set; }
        public double? AverageRating { get; set; }
        public int? OCOPRating { get; set; }
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        
        // 🔹 Khoảng cách từ vị trí người dùng (km) - chỉ có khi có tọa độ người dùng
        public double? Distance { get; set; }
        
        // 🔹 Số lượng đánh giá
        public int RatingCount { get; set; }
        
        // 🔹 URL để mở Google Maps chỉ đường
        public string? DirectionsUrl { get; set; }
    }
}

