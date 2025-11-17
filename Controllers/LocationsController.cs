using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using System.Net.Http;
using System.Text.Json;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private const string VIETNAM_API_BASE = "https://provinces.open-api.vn/api";

        public LocationsController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // 🔹 GET: api/locations - Xem tất cả địa điểm (public)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetLocations()
        {
            var locations = await _context.Locations.ToListAsync();

            var locationDtos = locations.Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                Address = l.Address,
                Latitude = l.Latitude,
                Longitude = l.Longitude
            });

            return Ok(locationDtos);
        }

        // 🔹 GET: api/locations/{id} - Xem chi tiết địa điểm (public)
        [HttpGet("{id}")]
        public async Task<ActionResult<LocationDto>> GetLocation(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound("Không tìm thấy địa điểm.");

            var locationDto = new LocationDto
            {
                Id = location.Id,
                Name = location.Name,
                Address = location.Address,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            };

            return Ok(locationDto);
        }

        // 🔹 POST: api/locations - Tạo địa điểm mới (chỉ SystemAdmin)
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost]
        public async Task<ActionResult<LocationDto>> CreateLocation([FromBody] CreateLocationDto dto)
        {
            // 🔹 Validation
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Tên địa điểm là bắt buộc.");

            if (string.IsNullOrWhiteSpace(dto.Address))
                return BadRequest("Địa chỉ là bắt buộc.");

            // 🔹 Validation: Latitude (-90 đến 90)
            if (dto.Latitude < -90 || dto.Latitude > 90)
                return BadRequest("Latitude (vĩ độ) phải nằm trong khoảng -90 đến 90.");

            // 🔹 Validation: Longitude (-180 đến 180)
            if (dto.Longitude < -180 || dto.Longitude > 180)
                return BadRequest("Longitude (kinh độ) phải nằm trong khoảng -180 đến 180.");

            var location = new Location
            {
                Name = dto.Name.Trim(),
                Address = dto.Address.Trim(),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            var locationDto = new LocationDto
            {
                Id = location.Id,
                Name = location.Name,
                Address = location.Address,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            };

            return CreatedAtAction(nameof(GetLocation), new { id = location.Id }, locationDto);
        }

        // 🔹 PUT: api/locations/{id} - Cập nhật địa điểm (chỉ SystemAdmin)
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] CreateLocationDto dto)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound("Không tìm thấy địa điểm.");

            // 🔹 Validation
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Tên địa điểm là bắt buộc.");

            if (string.IsNullOrWhiteSpace(dto.Address))
                return BadRequest("Địa chỉ là bắt buộc.");

            // 🔹 Validation: Latitude
            if (dto.Latitude < -90 || dto.Latitude > 90)
                return BadRequest("Latitude (vĩ độ) phải nằm trong khoảng -90 đến 90.");

            // 🔹 Validation: Longitude
            if (dto.Longitude < -180 || dto.Longitude > 180)
                return BadRequest("Longitude (kinh độ) phải nằm trong khoảng -180 đến 180.");

            location.Name = dto.Name.Trim();
            location.Address = dto.Address.Trim();
            location.Latitude = dto.Latitude;
            location.Longitude = dto.Longitude;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 🔹 DELETE: api/locations/{id} - Xóa địa điểm (chỉ SystemAdmin)
        [Authorize(Roles = "SystemAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound("Không tìm thấy địa điểm.");

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 🔹 GET: api/locations/provinces - Lấy danh sách tất cả tỉnh/thành phố
        [HttpGet("provinces")]
        public ActionResult<IEnumerable<ProvinceLocationDto>> GetProvinces()
        {
            // Dữ liệu tỉnh/thành phố của Việt Nam (63 tỉnh/thành phố)
            var provinces = new Dictionary<string, string>
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

            var provinceDtos = provinces.Select(p => new ProvinceLocationDto
            {
                Code = p.Key,
                Name = p.Value
            }).OrderBy(p => p.Name).ToList();

            return Ok(provinceDtos);
        }

        // 🔹 GET: api/locations/districts?provinceCode=... - Lấy danh sách quận/huyện theo tỉnh/thành phố
        // 🔒 SECURITY: Sử dụng API công khai để lấy dữ liệu (backend gọi, tránh CORS)
        [HttpGet("districts")]
        public async Task<ActionResult<IEnumerable<ProvinceLocationDto>>> GetDistricts([FromQuery] string? provinceCode)
        {
            if (string.IsNullOrWhiteSpace(provinceCode))
            {
                return BadRequest("Vui lòng cung cấp mã tỉnh/thành phố.");
            }

            try
            {
                // 🔹 Gọi API công khai của Vietnam Address API
                var url = $"{VIETNAM_API_BASE}/p/{provinceCode}?depth=2";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, $"Không thể lấy dữ liệu từ API công khai: {response.StatusCode}");
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                // API trả về province object với districts array
                var districts = new List<ProvinceLocationDto>();
                
                if (root.TryGetProperty("districts", out var districtsElement) && districtsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var district in districtsElement.EnumerateArray())
                    {
                        var code = district.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : "";
                        var name = district.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : "";

                        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
                        {
                            districts.Add(new ProvinceLocationDto
                            {
                                Code = code,
                                Name = name
                            });
                        }
                    }
                }

                return Ok(districts);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, $"Lỗi kết nối đến API công khai: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "Request timeout khi gọi API công khai");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xử lý dữ liệu: {ex.Message}");
            }
        }

        // 🔹 GET: api/locations/wards?provinceCode=...&districtCode=... - Lấy danh sách phường/xã theo quận/huyện
        // 🔒 SECURITY: Sử dụng API công khai để lấy dữ liệu (backend gọi, tránh CORS)
        [HttpGet("wards")]
        public async Task<ActionResult<IEnumerable<ProvinceLocationDto>>> GetWards([FromQuery] string? provinceCode, [FromQuery] string? districtCode)
        {
            if (string.IsNullOrWhiteSpace(provinceCode) || string.IsNullOrWhiteSpace(districtCode))
            {
                return BadRequest("Vui lòng cung cấp mã tỉnh/thành phố và mã quận/huyện.");
            }

            try
            {
                // 🔹 Gọi API công khai của Vietnam Address API
                var url = $"{VIETNAM_API_BASE}/d/{districtCode}?depth=2";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, $"Không thể lấy dữ liệu từ API công khai: {response.StatusCode}");
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                // API trả về district object với wards array
                var wards = new List<ProvinceLocationDto>();
                
                if (root.TryGetProperty("wards", out var wardsElement) && wardsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ward in wardsElement.EnumerateArray())
                    {
                        var code = ward.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : "";
                        var name = ward.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : "";

                        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
                        {
                            wards.Add(new ProvinceLocationDto
                            {
                                Code = code,
                                Name = name
                            });
                        }
                    }
                }

                return Ok(wards);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, $"Lỗi kết nối đến API công khai: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "Request timeout khi gọi API công khai");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xử lý dữ liệu: {ex.Message}");
            }
        }
    }

    // DTO cho provinces/districts/wards
    public class ProvinceLocationDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
