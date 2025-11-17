using Microsoft.AspNetCore.Mvc;

namespace GiaLaiOCOP.Api.Controllers
{
    /// <summary>
    /// API để lấy danh sách tỉnh/thành phố, quận/huyện, phường/xã của Việt Nam
    /// Sử dụng dữ liệu từ API công khai hoặc file JSON
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        // 🔹 Dữ liệu tỉnh/thành phố của Việt Nam (63 tỉnh/thành phố)
        private static readonly Dictionary<string, string> Provinces = new()
        {
            { "01", "An Giang" },
            { "02", "Bà Rịa - Vũng Tàu" },
            { "03", "Bắc Giang" },
            { "04", "Bắc Kạn" },
            { "05", "Bạc Liêu" },
            { "06", "Bắc Ninh" },
            { "07", "Bến Tre" },
            { "08", "Bình Định" },
            { "09", "Bình Dương" },
            { "10", "Bình Phước" },
            { "11", "Bình Thuận" },
            { "12", "Cà Mau" },
            { "13", "Cao Bằng" },
            { "14", "Đắk Lắk" },
            { "15", "Đắk Nông" },
            { "16", "Điện Biên" },
            { "17", "Đồng Nai" },
            { "18", "Đồng Tháp" },
            { "19", "Gia Lai" },
            { "20", "Hà Giang" },
            { "21", "Hà Nam" },
            { "22", "Hà Tĩnh" },
            { "23", "Hải Dương" },
            { "24", "Hải Phòng" },
            { "25", "Hậu Giang" },
            { "26", "Hòa Bình" },
            { "27", "Hưng Yên" },
            { "28", "Khánh Hòa" },
            { "29", "Kiên Giang" },
            { "30", "Kon Tum" },
            { "31", "Lai Châu" },
            { "32", "Lâm Đồng" },
            { "33", "Lạng Sơn" },
            { "34", "Lào Cai" },
            { "35", "Long An" },
            { "36", "Nam Định" },
            { "37", "Nghệ An" },
            { "38", "Ninh Bình" },
            { "39", "Ninh Thuận" },
            { "40", "Phú Thọ" },
            { "41", "Phú Yên" },
            { "42", "Quảng Bình" },
            { "43", "Quảng Nam" },
            { "44", "Quảng Ngãi" },
            { "45", "Quảng Ninh" },
            { "46", "Quảng Trị" },
            { "47", "Sóc Trăng" },
            { "48", "Sơn La" },
            { "49", "Tây Ninh" },
            { "50", "Thái Bình" },
            { "51", "Thái Nguyên" },
            { "52", "Thanh Hóa" },
            { "53", "Thừa Thiên Huế" },
            { "54", "Tiền Giang" },
            { "55", "TP Hồ Chí Minh" },
            { "56", "Trà Vinh" },
            { "57", "Tuyên Quang" },
            { "58", "Vĩnh Long" },
            { "59", "Vĩnh Phúc" },
            { "60", "Yên Bái" },
            { "61", "TP Hà Nội" },
            { "62", "TP Cần Thơ" },
            { "63", "TP Đà Nẵng" }
        };

        /// <summary>
        /// GET: api/locations/provinces - Lấy danh sách tất cả tỉnh/thành phố
        /// </summary>
        [HttpGet("provinces")]
        public ActionResult<IEnumerable<LocationDto>> GetProvinces()
        {
            var provinces = Provinces.Select(p => new LocationDto
            {
                Code = p.Key,
                Name = p.Value
            }).OrderBy(p => p.Name).ToList();

            return Ok(provinces);
        }

        /// <summary>
        /// GET: api/locations/districts?provinceCode=... - Lấy danh sách quận/huyện theo tỉnh/thành phố
        /// Note: Đây là API đơn giản, trong thực tế cần có dữ liệu đầy đủ về quận/huyện
        /// </summary>
        [HttpGet("districts")]
        public ActionResult<IEnumerable<LocationDto>> GetDistricts([FromQuery] string? provinceCode)
        {
            // 🔹 Tạm thời trả về danh sách rỗng hoặc dữ liệu mẫu
            // Trong thực tế, cần có file JSON hoặc database chứa dữ liệu quận/huyện
            // Có thể sử dụng API công khai: https://provinces.open-api.vn/api/d/?p={provinceCode}
            
            if (string.IsNullOrWhiteSpace(provinceCode))
            {
                return BadRequest("Vui lòng cung cấp mã tỉnh/thành phố.");
            }

            // 🔹 Gọi API công khai của Vietnam Address API
            // Tạm thời trả về empty list, sẽ implement sau hoặc dùng API công khai
            return Ok(new List<LocationDto>());
        }

        /// <summary>
        /// GET: api/locations/wards?provinceCode=...&districtCode=... - Lấy danh sách phường/xã theo quận/huyện
        /// Note: Đây là API đơn giản, trong thực tế cần có dữ liệu đầy đủ về phường/xã
        /// </summary>
        [HttpGet("wards")]
        public ActionResult<IEnumerable<LocationDto>> GetWards([FromQuery] string? provinceCode, [FromQuery] string? districtCode)
        {
            if (string.IsNullOrWhiteSpace(provinceCode) || string.IsNullOrWhiteSpace(districtCode))
            {
                return BadRequest("Vui lòng cung cấp mã tỉnh/thành phố và mã quận/huyện.");
            }

            // 🔹 Gọi API công khai của Vietnam Address API
            // Tạm thời trả về empty list, sẽ implement sau hoặc dùng API công khai
            return Ok(new List<LocationDto>());
        }
    }

    public class LocationDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
