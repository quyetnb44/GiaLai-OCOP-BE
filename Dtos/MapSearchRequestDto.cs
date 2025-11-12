namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO cho request tìm kiếm trên map
    /// </summary>
    public class MapSearchRequestDto
    {
        public string? Keyword { get; set; }                    // Từ khóa tìm kiếm
        public string? District { get; set; }                   // Huyện/xã
        public string? Province { get; set; }                   // Tỉnh/thành phố
        public int? OCOPRating { get; set; }                    // Xếp hạng OCOP (3-5)
        public string? BusinessField { get; set; }              // Ngành hàng
        
        // 🔹 Bounding box (cho FR-MAP-02)
        public double? MinLatitude { get; set; }
        public double? MaxLatitude { get; set; }
        public double? MinLongitude { get; set; }
        public double? MaxLongitude { get; set; }
        
        // 🔹 Tìm kiếm theo tọa độ và bán kính (cho FR-MAP-08)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Radius { get; set; }                     // Bán kính tính bằng km
        
        // 🔹 Lọc theo khoảng cách từ vị trí người dùng
        public double? UserLatitude { get; set; }
        public double? UserLongitude { get; set; }
        public double? MaxDistance { get; set; }                // Khoảng cách tối đa (km)
        
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        
        // 🔹 Sorting
        public string? SortBy { get; set; } = "name"; // name, distance, rating, ocopRating
        public string? SortOrder { get; set; } = "asc"; // asc, desc
        
        // 🔹 Tọa độ người dùng để tính khoảng cách và tạo directions URL
        public double? UserLat { get; set; }
        public double? UserLng { get; set; }
    }
}

