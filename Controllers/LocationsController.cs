using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        private readonly HttpClient _httpClient;
        private const string VIETNAM_API_BASE = "https://provinces.open-api.vn/api";

        public LocationsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
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
        /// Sử dụng API công khai: https://provinces.open-api.vn/api
        /// </summary>
        [HttpGet("districts")]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetDistricts([FromQuery] string? provinceCode)
        {
            if (string.IsNullOrWhiteSpace(provinceCode))
            {
                return BadRequest("Vui lòng cung cấp mã tỉnh/thành phố.");
            }

            try
            {
                // 🔹 Gọi API công khai của Vietnam Address API
                var url = $"{VIETNAM_API_BASE}/p/{provinceCode}?depth=2";
                
                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.GetAsync(url);
                }
                catch (HttpRequestException httpEx)
                {
                    return StatusCode(503, $"Lỗi kết nối đến API công khai: {httpEx.Message}");
                }
                catch (TaskCanceledException)
                {
                    return StatusCode(504, "Request timeout khi gọi API công khai");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, $"Không thể lấy dữ liệu từ API công khai: {response.StatusCode} - {errorContent}");
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    return StatusCode(500, "API công khai trả về dữ liệu rỗng");
                }

                JsonDocument? jsonDoc = null;
                try
                {
                    jsonDoc = JsonDocument.Parse(jsonString);
                }
                catch (JsonException jsonEx)
                {
                    // Log chi tiết để debug
                    var preview = jsonString.Length > 500 ? jsonString.Substring(0, 500) + "..." : jsonString;
                    return StatusCode(500, $"Lỗi parse JSON từ API công khai: {jsonEx.Message}. Response preview: {preview}");
                }

                using (jsonDoc)
                {
                    var root = jsonDoc.RootElement;
                    var districts = new List<LocationDto>();
                    
                    // 🔹 API công khai có thể trả về nhiều format:
                    // Format 1: { "code": "...", "name": "...", "districts": [...] }
                    // Format 2: [{ "code": "...", "name": "..." }, ...] (array districts trực tiếp)
                    
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        // Format 1: Object với districts property
                        if (root.TryGetProperty("districts", out var districtsElement) && districtsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var district in districtsElement.EnumerateArray())
                            {
                                var code = district.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : 
                                          (district.TryGetProperty("codename", out var codeNameElement) ? codeNameElement.GetString() : "");
                                var name = district.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : "";

                                if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
                                {
                                    districts.Add(new LocationDto
                                    {
                                        Code = code,
                                        Name = name
                                    });
                                }
                            }
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        // Format 2: Array trực tiếp
                        foreach (var item in root.EnumerateArray())
                        {
                            var code = item.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : 
                                      (item.TryGetProperty("codename", out var codeNameElement) ? codeNameElement.GetString() : "");
                            var name = item.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : "";

                            if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
                            {
                                districts.Add(new LocationDto
                                {
                                    Code = code,
                                    Name = name
                                });
                            }
                        }
                    }

                    return Ok(districts);
                }
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
                return StatusCode(500, $"Lỗi khi xử lý dữ liệu: {ex.Message}. StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// GET: api/locations/wards?provinceCode=...&districtCode=... - Lấy danh sách phường/xã theo quận/huyện
        /// Sử dụng API công khai: https://provinces.open-api.vn/api
        /// </summary>
        [HttpGet("wards")]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetWards([FromQuery] string? provinceCode, [FromQuery] string? districtCode)
        {
            if (string.IsNullOrWhiteSpace(provinceCode) || string.IsNullOrWhiteSpace(districtCode))
            {
                return BadRequest("Vui lòng cung cấp mã tỉnh/thành phố và mã quận/huyện.");
            }

            try
            {
                // 🔹 Gọi API công khai của Vietnam Address API
                var url = $"{VIETNAM_API_BASE}/d/{districtCode}?depth=2";
                
                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.GetAsync(url);
                }
                catch (HttpRequestException httpEx)
                {
                    return StatusCode(503, $"Lỗi kết nối đến API công khai: {httpEx.Message}");
                }
                catch (TaskCanceledException)
                {
                    return StatusCode(504, "Request timeout khi gọi API công khai");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, $"Không thể lấy dữ liệu từ API công khai: {response.StatusCode} - {errorContent}");
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    return StatusCode(500, "API công khai trả về dữ liệu rỗng");
                }

                JsonDocument? jsonDoc = null;
                try
                {
                    jsonDoc = JsonDocument.Parse(jsonString);
                }
                catch (JsonException jsonEx)
                {
                    // Log chi tiết để debug
                    var preview = jsonString.Length > 500 ? jsonString.Substring(0, 500) + "..." : jsonString;
                    return StatusCode(500, $"Lỗi parse JSON từ API công khai: {jsonEx.Message}. Response preview: {preview}");
                }

                using (jsonDoc)
                {
                    var root = jsonDoc.RootElement;
                    var wards = new List<LocationDto>();
                    
                    // 🔹 API công khai có thể trả về nhiều format:
                    // Format 1: { "code": "...", "name": "...", "wards": [...] }
                    // Format 2: [{ "code": "...", "name": "..." }, ...] (array wards trực tiếp)
                    
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        // Format 1: Object với wards property
                        if (root.TryGetProperty("wards", out var wardsElement) && wardsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var ward in wardsElement.EnumerateArray())
                            {
                                var code = ward.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : 
                                          (ward.TryGetProperty("codename", out var codeNameElement) ? codeNameElement.GetString() : "");
                                var name = ward.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : "";

                                if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
                                {
                                    wards.Add(new LocationDto
                                    {
                                        Code = code,
                                        Name = name
                                    });
                                }
                            }
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        // Format 2: Array trực tiếp
                        foreach (var item in root.EnumerateArray())
                        {
                            var code = item.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : 
                                      (item.TryGetProperty("codename", out var codeNameElement) ? codeNameElement.GetString() : "");
                            var name = item.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : "";

                            if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
                            {
                                wards.Add(new LocationDto
                                {
                                    Code = code,
                                    Name = name
                                });
                            }
                        }
                    }

                    return Ok(wards);
                }
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
                return StatusCode(500, $"Lỗi khi xử lý dữ liệu: {ex.Message}. StackTrace: {ex.StackTrace}");
            }
        }
    }

    public class LocationDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
