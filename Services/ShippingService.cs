using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GiaLaiOCOP.Api.Services
{
    public interface IShippingService
    {
        Task<(decimal fee, string zoneType, string zoneName)> CalculateShippingFeeAsync(string buyerProvince, string? sellerProvince = null);
        Task<List<ShippingRule>> GetAllShippingRulesAsync();
        string DetermineZoneType(string buyerProvince, string sellerProvince);
        string GetRegion(string province);
    }

    public class ShippingService : IShippingService
    {
        private readonly AppDbContext _context;
        
        // Địa chỉ người bán cố định: Gia Lai
        private const string DefaultSellerProvince = "Gia Lai";

        // Phân loại miền theo tỉnh/thành phố
        private static readonly Dictionary<string, string> ProvinceRegions = new(StringComparer.OrdinalIgnoreCase)
        {
            // Miền Bắc (28 tỉnh/thành phố)
            { "Hà Nội", "North" },
            { "Hà Giang", "North" },
            { "Cao Bằng", "North" },
            { "Bắc Kạn", "North" },
            { "Tuyên Quang", "North" },
            { "Lào Cai", "North" },
            { "Điện Biên", "North" },
            { "Lai Châu", "North" },
            { "Sơn La", "North" },
            { "Yên Bái", "North" },
            { "Hòa Bình", "North" },
            { "Thái Nguyên", "North" },
            { "Lạng Sơn", "North" },
            { "Quảng Ninh", "North" },
            { "Bắc Giang", "North" },
            { "Phú Thọ", "North" },
            { "Vĩnh Phúc", "North" },
            { "Bắc Ninh", "North" },
            { "Hải Dương", "North" },
            { "Hải Phòng", "North" },
            { "Hưng Yên", "North" },
            { "Thái Bình", "North" },
            { "Hà Nam", "North" },
            { "Nam Định", "North" },
            { "Ninh Bình", "North" },

            // Miền Trung (19 tỉnh/thành phố) - bao gồm Tây Nguyên
            { "Thanh Hóa", "Central" },
            { "Nghệ An", "Central" },
            { "Hà Tĩnh", "Central" },
            { "Quảng Bình", "Central" },
            { "Quảng Trị", "Central" },
            { "Thừa Thiên Huế", "Central" },
            { "Đà Nẵng", "Central" },
            { "Quảng Nam", "Central" },
            { "Quảng Ngãi", "Central" },
            { "Bình Định", "Central" },
            { "Phú Yên", "Central" },
            { "Khánh Hòa", "Central" },
            { "Ninh Thuận", "Central" },
            { "Bình Thuận", "Central" },
            // Tây Nguyên (thuộc miền Trung)
            { "Kon Tum", "Central" },
            { "Gia Lai", "Central" },
            { "Đắk Lắk", "Central" },
            { "Đắk Nông", "Central" },
            { "Lâm Đồng", "Central" },

            // Miền Nam (17 tỉnh/thành phố)
            { "Bình Phước", "South" },
            { "Tây Ninh", "South" },
            { "Bình Dương", "South" },
            { "Đồng Nai", "South" },
            { "Bà Rịa - Vũng Tàu", "South" },
            { "Hồ Chí Minh", "South" },
            { "TP. Hồ Chí Minh", "South" },
            { "TP Hồ Chí Minh", "South" },
            { "Long An", "South" },
            { "Tiền Giang", "South" },
            { "Bến Tre", "South" },
            { "Trà Vinh", "South" },
            { "Vĩnh Long", "South" },
            { "Đồng Tháp", "South" },
            { "An Giang", "South" },
            { "Kiên Giang", "South" },
            { "Cần Thơ", "South" },
            { "Hậu Giang", "South" },
            { "Sóc Trăng", "South" },
            { "Bạc Liêu", "South" },
            { "Cà Mau", "South" }
        };

        public ShippingService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tính phí vận chuyển dựa trên địa chỉ người mua
        /// </summary>
        public async Task<(decimal fee, string zoneType, string zoneName)> CalculateShippingFeeAsync(
            string buyerProvince, 
            string? sellerProvince = null)
        {
            sellerProvince ??= DefaultSellerProvince;

            var zoneType = DetermineZoneType(buyerProvince, sellerProvince);
            
            var rule = await _context.ShippingRules
                .FirstOrDefaultAsync(r => r.ZoneType == zoneType && r.IsActive);

            if (rule == null)
            {
                // Fallback: Nếu không tìm thấy rule, dùng giá mặc định
                return zoneType switch
                {
                    "SameProvince" => (20000m, zoneType, "Cùng tỉnh"),
                    "SameRegion" => (30000m, zoneType, "Cùng miền"),
                    "DifferentRegion" => (40000m, zoneType, "Khác miền"),
                    _ => (40000m, "DifferentRegion", "Khác miền")
                };
            }

            return (rule.ShippingFee, rule.ZoneType, rule.DisplayName);
        }

        /// <summary>
        /// Xác định loại vùng ship: cùng tỉnh, cùng miền, khác miền
        /// </summary>
        public string DetermineZoneType(string buyerProvince, string sellerProvince)
        {
            if (string.IsNullOrWhiteSpace(buyerProvince))
                return "DifferentRegion";

            // Normalize tên tỉnh
            var normalizedBuyer = NormalizeProvinceName(buyerProvince);
            var normalizedSeller = NormalizeProvinceName(sellerProvince);

            // Cùng tỉnh
            if (normalizedBuyer.Equals(normalizedSeller, StringComparison.OrdinalIgnoreCase))
                return "SameProvince";

            // Lấy miền
            var buyerRegion = GetRegion(normalizedBuyer);
            var sellerRegion = GetRegion(normalizedSeller);

            // Cùng miền
            if (buyerRegion == sellerRegion)
                return "SameRegion";

            // Khác miền
            return "DifferentRegion";
        }

        /// <summary>
        /// Lấy miền của tỉnh/thành phố
        /// </summary>
        public string GetRegion(string province)
        {
            var normalized = NormalizeProvinceName(province);
            
            if (ProvinceRegions.TryGetValue(normalized, out var region))
                return region;

            // Thử tìm với các biến thể
            foreach (var kvp in ProvinceRegions)
            {
                if (kvp.Key.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                    normalized.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            // Mặc định: coi như khác miền (Central - vì seller ở Gia Lai)
            return "Unknown";
        }

        /// <summary>
        /// Lấy tất cả shipping rules
        /// </summary>
        public async Task<List<ShippingRule>> GetAllShippingRulesAsync()
        {
            return await _context.ShippingRules
                .Where(r => r.IsActive)
                .OrderBy(r => r.ShippingFee)
                .ToListAsync();
        }

        /// <summary>
        /// Chuẩn hóa tên tỉnh (bỏ tiền tố "Tỉnh", "Thành phố", ...)
        /// </summary>
        private static string NormalizeProvinceName(string province)
        {
            if (string.IsNullOrWhiteSpace(province))
                return string.Empty;

            var normalized = province.Trim();
            
            // Bỏ các tiền tố phổ biến
            var prefixes = new[] { "Tỉnh ", "Thành phố ", "TP. ", "TP " };
            foreach (var prefix in prefixes)
            {
                if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = normalized[prefix.Length..].Trim();
                    break;
                }
            }

            return normalized;
        }
    }
}

