using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using System.ComponentModel.DataAnnotations;

namespace GiaLaiOCOP.Api.Controllers
{
    /// <summary>
    /// API Map - Quản lý bản đồ doanh nghiệp OCOP
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class MapController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MapController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// FR-MAP-01: Tìm kiếm doanh nghiệp OCOP theo từ khóa
        /// </summary>
        /// <param name="keyword">Từ khóa tìm kiếm (tên doanh nghiệp, sản phẩm, địa chỉ...)</param>
        /// <param name="userLat">Vĩ độ của người dùng (để tính khoảng cách)</param>
        /// <param name="userLng">Kinh độ của người dùng (để tính khoảng cách)</param>
        /// <param name="page">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Số lượng mỗi trang (mặc định: 20, tối đa: 100)</param>
        /// <param name="sortBy">Sắp xếp theo: name, distance, rating, ocopRating (mặc định: name)</param>
        /// <param name="sortOrder">Thứ tự: asc, desc (mặc định: asc)</param>
        /// <returns>Danh sách doanh nghiệp phù hợp</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(MapResponseDto<EnterpriseMapDto>), 200)]
        public async Task<ActionResult<MapResponseDto<EnterpriseMapDto>>> SearchEnterprises(
            [FromQuery] string? keyword,
            [FromQuery] double? userLat,
            [FromQuery] double? userLng,
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, 100)] int pageSize = 20,
            [FromQuery] string? sortBy = "name",
            [FromQuery] string? sortOrder = "asc")
        {
            var query = _context.Enterprises
                .Where(e => e.Latitude != null && e.Longitude != null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(e =>
                    e.Name.ToLower().Contains(keyword) ||
                    e.Description.ToLower().Contains(keyword) ||
                    e.BusinessField.ToLower().Contains(keyword) ||
                    e.Address.ToLower().Contains(keyword) ||
                    e.Products!.Any(p => p.Name.ToLower().Contains(keyword))
                );
            }

            var enterprises = await query
                .Include(e => e.Products!)
                    .ThenInclude(p => p.Reviews)
                .ToListAsync();

            // Map to DTO với distance và rating count
            var enterprisesDto = enterprises.Select(e => MapToEnterpriseMapDto(e, userLat, userLng)).ToList();

            // Sorting
            enterprisesDto = ApplySorting(enterprisesDto, sortBy, sortOrder, userLat, userLng);

            // Pagination
            var total = enterprisesDto.Count;
            var pagedResults = enterprisesDto
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new MapResponseDto<EnterpriseMapDto>
            {
                Data = pagedResults,
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// FR-MAP-02: Tìm doanh nghiệp theo khu vực bản đồ (bounding box)
        /// </summary>
        /// <param name="minLatitude">Vĩ độ tối thiểu</param>
        /// <param name="maxLatitude">Vĩ độ tối đa</param>
        /// <param name="minLongitude">Kinh độ tối thiểu</param>
        /// <param name="maxLongitude">Kinh độ tối đa</param>
        /// <param name="userLat">Vĩ độ của người dùng</param>
        /// <param name="userLng">Kinh độ của người dùng</param>
        /// <param name="page">Số trang</param>
        /// <param name="pageSize">Số lượng mỗi trang</param>
        /// <param name="sortBy">Sắp xếp theo</param>
        /// <param name="sortOrder">Thứ tự</param>
        /// <returns>Danh sách doanh nghiệp trong vùng bản đồ</returns>
        [HttpGet("bounding-box")]
        [ProducesResponseType(typeof(MapResponseDto<EnterpriseMapDto>), 200)]
        public async Task<ActionResult<MapResponseDto<EnterpriseMapDto>>> GetEnterprisesByBoundingBox(
            [FromQuery, Required] double minLatitude,
            [FromQuery, Required] double maxLatitude,
            [FromQuery, Required] double minLongitude,
            [FromQuery, Required] double maxLongitude,
            [FromQuery] double? userLat,
            [FromQuery] double? userLng,
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, 100)] int pageSize = 20,
            [FromQuery] string? sortBy = "name",
            [FromQuery] string? sortOrder = "asc")
        {
            // Validation
            if (minLatitude < -90 || maxLatitude > 90 || minLatitude > maxLatitude)
                return BadRequest(new { Error = "Latitude không hợp lệ. Phải từ -90 đến 90 và minLatitude < maxLatitude." });

            if (minLongitude < -180 || maxLongitude > 180 || minLongitude > maxLongitude)
                return BadRequest(new { Error = "Longitude không hợp lệ. Phải từ -180 đến 180 và minLongitude < maxLongitude." });

            var enterprises = await _context.Enterprises
                .Where(e => e.Latitude != null && e.Longitude != null &&
                           e.Latitude >= minLatitude && e.Latitude <= maxLatitude &&
                           e.Longitude >= minLongitude && e.Longitude <= maxLongitude)
                .Include(e => e.Products!)
                    .ThenInclude(p => p.Reviews)
                .ToListAsync();

            var enterprisesDto = enterprises.Select(e => MapToEnterpriseMapDto(e, userLat, userLng)).ToList();
            enterprisesDto = ApplySorting(enterprisesDto, sortBy, sortOrder, userLat, userLng);

            var total = enterprisesDto.Count;
            var pagedResults = enterprisesDto
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new MapResponseDto<EnterpriseMapDto>
            {
                Data = pagedResults,
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// FR-MAP-08: Tìm kiếm doanh nghiệp theo tọa độ và bán kính
        /// </summary>
        /// <param name="latitude">Vĩ độ</param>
        /// <param name="longitude">Kinh độ</param>
        /// <param name="radius">Bán kính (km, mặc định: 10, tối đa: 100)</param>
        /// <param name="page">Số trang</param>
        /// <param name="pageSize">Số lượng mỗi trang</param>
        /// <param name="sortBy">Sắp xếp theo (mặc định: distance)</param>
        /// <param name="sortOrder">Thứ tự</param>
        /// <returns>Danh sách doanh nghiệp gần nhất</returns>
        [HttpGet("nearby")]
        [ProducesResponseType(typeof(MapResponseDto<EnterpriseMapDto>), 200)]
        public async Task<ActionResult<MapResponseDto<EnterpriseMapDto>>> GetNearbyEnterprises(
            [FromQuery, Required] double latitude,
            [FromQuery, Required] double longitude,
            [FromQuery, Range(0.1, 100)] double radius = 10,
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, 100)] int pageSize = 20,
            [FromQuery] string? sortBy = "distance",
            [FromQuery] string? sortOrder = "asc")
        {
            // Validation
            if (latitude < -90 || latitude > 90)
                return BadRequest(new { Error = "Latitude phải nằm trong khoảng -90 đến 90." });

            if (longitude < -180 || longitude > 180)
                return BadRequest(new { Error = "Longitude phải nằm trong khoảng -180 đến 180." });

            var enterprises = await _context.Enterprises
                .Where(e => e.Latitude != null && e.Longitude != null)
                .Include(e => e.Products!)
                    .ThenInclude(p => p.Reviews)
                .ToListAsync();

            // Tính khoảng cách và lọc
            var nearbyEnterprises = enterprises
                .Where(e => e.Latitude.HasValue && e.Longitude.HasValue)
                .Select(e => new
                {
                    Enterprise = e,
                    Distance = CalculateDistance(latitude, longitude, e.Latitude!.Value, e.Longitude!.Value)
                })
                .Where(x => x.Distance <= radius)
                .Select(x => MapToEnterpriseMapDto(x.Enterprise, latitude, longitude, x.Distance))
                .ToList();

            // Sorting (mặc định sort by distance)
            nearbyEnterprises = ApplySorting(nearbyEnterprises, sortBy, sortOrder, latitude, longitude);

            var total = nearbyEnterprises.Count;
            var pagedResults = nearbyEnterprises
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new MapResponseDto<EnterpriseMapDto>
            {
                Data = pagedResults,
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// FR-MAP-06: Lọc doanh nghiệp theo nhiều điều kiện
        /// </summary>
        /// <param name="request">Đối tượng chứa các điều kiện lọc</param>
        /// <returns>Danh sách doanh nghiệp đã lọc</returns>
        [HttpGet("filter")]
        [ProducesResponseType(typeof(MapResponseDto<EnterpriseMapDto>), 200)]
        public async Task<ActionResult<MapResponseDto<EnterpriseMapDto>>> FilterEnterprises([FromQuery] MapSearchRequestDto request)
        {
            var query = _context.Enterprises
                .Where(e => e.Latitude != null && e.Longitude != null)
                .AsQueryable();

            // Lọc theo huyện/xã
            if (!string.IsNullOrWhiteSpace(request.District))
            {
                query = query.Where(e => e.District.ToLower().Contains(request.District.Trim().ToLower()));
            }

            // Lọc theo tỉnh/thành phố
            if (!string.IsNullOrWhiteSpace(request.Province))
            {
                query = query.Where(e => e.Province.ToLower().Contains(request.Province.Trim().ToLower()));
            }

            // Lọc theo xếp hạng OCOP
            if (request.OCOPRating.HasValue && request.OCOPRating.Value >= 3 && request.OCOPRating.Value <= 5)
            {
                query = query.Where(e => e.OCOPRating == request.OCOPRating.Value);
            }

            // Lọc theo ngành hàng
            if (!string.IsNullOrWhiteSpace(request.BusinessField))
            {
                query = query.Where(e => e.BusinessField.ToLower().Contains(request.BusinessField.Trim().ToLower()));
            }

            // Lọc theo từ khóa
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLower();
                query = query.Where(e =>
                    e.Name.ToLower().Contains(keyword) ||
                    e.Description.ToLower().Contains(keyword) ||
                    e.BusinessField.ToLower().Contains(keyword) ||
                    e.Address.ToLower().Contains(keyword) ||
                    e.Products!.Any(p => p.Name.ToLower().Contains(keyword))
                );
            }

            // Lọc theo bounding box
            if (request.MinLatitude.HasValue && request.MaxLatitude.HasValue &&
                request.MinLongitude.HasValue && request.MaxLongitude.HasValue)
            {
                query = query.Where(e =>
                    e.Latitude >= request.MinLatitude && e.Latitude <= request.MaxLatitude &&
                    e.Longitude >= request.MinLongitude && e.Longitude <= request.MaxLongitude);
            }

            var enterprises = await query
                .Include(e => e.Products!)
                    .ThenInclude(p => p.Reviews)
                .ToListAsync();

            // Lọc theo khoảng cách từ vị trí người dùng
            if (request.UserLatitude.HasValue && request.UserLongitude.HasValue && request.MaxDistance.HasValue)
            {
                enterprises = enterprises
                    .Where(e => e.Latitude.HasValue && e.Longitude.HasValue)
                    .Where(e =>
                    {
                        var distance = CalculateDistance(
                            request.UserLatitude.Value,
                            request.UserLongitude.Value,
                            e.Latitude!.Value,
                            e.Longitude!.Value);
                        return distance <= request.MaxDistance.Value;
                    })
                    .ToList();
            }

            var enterprisesDto = enterprises.Select(e => MapToEnterpriseMapDto(e, request.UserLat, request.UserLng)).ToList();
            enterprisesDto = ApplySorting(enterprisesDto, request.SortBy ?? "name", request.SortOrder ?? "asc", request.UserLat, request.UserLng);

            var total = enterprisesDto.Count;
            var pagedResults = enterprisesDto
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return Ok(new MapResponseDto<EnterpriseMapDto>
            {
                Data = pagedResults,
                Total = total,
                Page = request.Page,
                PageSize = request.PageSize
            });
        }

        /// <summary>
        /// FR-MAP-04: Lấy chi tiết doanh nghiệp khi click marker
        /// </summary>
        /// <param name="id">ID doanh nghiệp</param>
        /// <param name="userLat">Vĩ độ của người dùng (tùy chọn)</param>
        /// <param name="userLng">Kinh độ của người dùng (tùy chọn)</param>
        /// <returns>Thông tin chi tiết doanh nghiệp</returns>
        [HttpGet("enterprises/{id}")]
        [ProducesResponseType(typeof(EnterpriseDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EnterpriseDetailDto>> GetEnterpriseDetail(
            int id,
            [FromQuery] double? userLat,
            [FromQuery] double? userLng)
        {
            var enterprise = await _context.Enterprises
                .Include(e => e.Products!)
                    .ThenInclude(p => p.Reviews)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enterprise == null)
                return NotFound(new { Error = "Không tìm thấy doanh nghiệp." });

            // Lấy 3 sản phẩm nổi bật
            var featuredProducts = enterprise.Products?
                .OrderByDescending(p => p.OCOPRating ?? 0)
                .ThenByDescending(p => CalculateAverageRating(p.Reviews) ?? 0)
                .Take(3)
                .Select(p => new ProductMapDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    OCOPRating = p.OCOPRating,
                    StockStatus = p.StockStatus,
                    AverageRating = CalculateAverageRating(p.Reviews),
                    EnterpriseId = p.EnterpriseId
                })
                .ToList() ?? new List<ProductMapDto>();

            // Tính rating count
            var ratingCount = enterprise.Products?
                .SelectMany(p => p.Reviews)
                .Count() ?? 0;

            // Tính distance nếu có tọa độ người dùng
            double? distance = null;
            if (userLat.HasValue && userLng.HasValue && enterprise.Latitude.HasValue && enterprise.Longitude.HasValue)
            {
                distance = CalculateDistance(userLat.Value, userLng.Value, enterprise.Latitude.Value, enterprise.Longitude.Value);
            }

            var enterpriseDetail = new EnterpriseDetailDto
            {
                Id = enterprise.Id,
                Name = enterprise.Name,
                Description = enterprise.Description,
                Address = enterprise.Address,
                Ward = enterprise.Ward,
                District = enterprise.District,
                Province = enterprise.Province,
                Latitude = enterprise.Latitude,
                Longitude = enterprise.Longitude,
                PhoneNumber = enterprise.PhoneNumber,
                EmailContact = enterprise.EmailContact,
                Website = enterprise.Website,
                ImageUrl = enterprise.ImageUrl,
                AverageRating = CalculateAverageRating(enterprise.Products),
                OCOPRating = enterprise.OCOPRating,
                BusinessField = enterprise.BusinessField,
                FeaturedProducts = featuredProducts,
                TotalProducts = enterprise.Products?.Count ?? 0,
                RatingCount = ratingCount,
                Distance = distance,
                DirectionsUrl = enterprise.Latitude.HasValue && enterprise.Longitude.HasValue
                    ? GenerateDirectionsUrl(enterprise.Latitude.Value, enterprise.Longitude.Value)
                    : null
            };

            return Ok(enterpriseDetail);
        }

        /// <summary>
        /// FR-MAP-05: Lấy danh sách sản phẩm của doanh nghiệp
        /// </summary>
        /// <param name="id">ID doanh nghiệp</param>
        /// <param name="page">Số trang</param>
        /// <param name="pageSize">Số lượng mỗi trang</param>
        /// <returns>Danh sách sản phẩm</returns>
        [HttpGet("enterprises/{id}/products")]
        [ProducesResponseType(typeof(MapResponseDto<ProductMapDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<MapResponseDto<ProductMapDto>>> GetEnterpriseProducts(
            int id,
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, 100)] int pageSize = 20)
        {
            var enterprise = await _context.Enterprises
                .Include(e => e.Products!)
                    .ThenInclude(p => p.Reviews)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enterprise == null)
                return NotFound(new { Error = "Không tìm thấy doanh nghiệp." });

            var products = enterprise.Products?
                .Select(p => new ProductMapDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    OCOPRating = p.OCOPRating,
                    StockStatus = p.StockStatus,
                    AverageRating = CalculateAverageRating(p.Reviews),
                    EnterpriseId = p.EnterpriseId
                })
                .ToList() ?? new List<ProductMapDto>();

            var total = products.Count;
            var pagedResults = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new MapResponseDto<ProductMapDto>
            {
                Data = pagedResults,
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Lấy danh sách các options cho filter (districts, provinces, business fields)
        /// </summary>
        /// <returns>Danh sách các options</returns>
        [HttpGet("filter-options")]
        [ProducesResponseType(typeof(FilterOptionsDto), 200)]
        public async Task<ActionResult<FilterOptionsDto>> GetFilterOptions()
        {
            var enterprises = await _context.Enterprises
                .Where(e => e.Latitude != null && e.Longitude != null)
                .ToListAsync();

            var districts = enterprises
                .Where(e => !string.IsNullOrWhiteSpace(e.District))
                .Select(e => e.District)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var provinces = enterprises
                .Where(e => !string.IsNullOrWhiteSpace(e.Province))
                .Select(e => e.Province)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            var businessFields = enterprises
                .Where(e => !string.IsNullOrWhiteSpace(e.BusinessField))
                .Select(e => e.BusinessField)
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            return Ok(new FilterOptionsDto
            {
                Districts = districts,
                Provinces = provinces,
                BusinessFields = businessFields,
                OCOPRatings = new List<int> { 3, 4, 5 }
            });
        }

        // ============================================
        // 🔹 Helper Methods
        // ============================================

        /// <summary>
        /// Map Enterprise to EnterpriseMapDto với distance và directions URL
        /// </summary>
        private EnterpriseMapDto MapToEnterpriseMapDto(Enterprise enterprise, double? userLat = null, double? userLng = null, double? preCalculatedDistance = null)
        {
            // Tính distance
            double? distance = preCalculatedDistance;
            if (!distance.HasValue && userLat.HasValue && userLng.HasValue && enterprise.Latitude.HasValue && enterprise.Longitude.HasValue)
            {
                distance = CalculateDistance(userLat.Value, userLng.Value, enterprise.Latitude.Value, enterprise.Longitude.Value);
            }

            // Tính rating count
            var ratingCount = enterprise.Products?
                .SelectMany(p => p.Reviews)
                .Count() ?? 0;

            return new EnterpriseMapDto
            {
                Id = enterprise.Id,
                Name = enterprise.Name,
                Address = enterprise.Address,
                Latitude = enterprise.Latitude,
                Longitude = enterprise.Longitude,
                ImageUrl = enterprise.ImageUrl,
                AverageRating = CalculateAverageRating(enterprise.Products),
                OCOPRating = enterprise.OCOPRating,
                District = enterprise.District,
                Province = enterprise.Province,
                Distance = distance,
                RatingCount = ratingCount,
                DirectionsUrl = enterprise.Latitude.HasValue && enterprise.Longitude.HasValue
                    ? GenerateDirectionsUrl(enterprise.Latitude.Value, enterprise.Longitude.Value)
                    : null
            };
        }

        /// <summary>
        /// Apply sorting cho danh sách enterprises
        /// </summary>
        private List<EnterpriseMapDto> ApplySorting(
            List<EnterpriseMapDto> enterprises,
            string? sortBy,
            string? sortOrder,
            double? userLat,
            double? userLng)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                sortBy = "name";

            var isAscending = string.IsNullOrWhiteSpace(sortOrder) || sortOrder.ToLower() == "asc";

            return sortBy.ToLower() switch
            {
                "distance" => isAscending
                    ? enterprises.OrderBy(e => e.Distance ?? double.MaxValue).ToList()
                    : enterprises.OrderByDescending(e => e.Distance ?? 0).ToList(),
                "rating" => isAscending
                    ? enterprises.OrderBy(e => e.AverageRating ?? 0).ToList()
                    : enterprises.OrderByDescending(e => e.AverageRating ?? 0).ToList(),
                "ocoprating" => isAscending
                    ? enterprises.OrderBy(e => e.OCOPRating ?? 0).ToList()
                    : enterprises.OrderByDescending(e => e.OCOPRating ?? 0).ToList(),
                "name" => isAscending
                    ? enterprises.OrderBy(e => e.Name).ToList()
                    : enterprises.OrderByDescending(e => e.Name).ToList(),
                _ => enterprises.OrderBy(e => e.Name).ToList()
            };
        }

        /// <summary>
        /// Tính khoảng cách giữa 2 điểm (Haversine formula) - trả về km
        /// </summary>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Bán kính Trái Đất (km)
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return Math.Round(R * c, 2); // Làm tròn 2 chữ số thập phân
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        /// <summary>
        /// Tính điểm đánh giá trung bình từ Reviews của các sản phẩm
        /// </summary>
        private double? CalculateAverageRating(ICollection<Product>? products)
        {
            if (products == null || !products.Any())
                return null;

            var allRatings = products
                .SelectMany(p => p.Reviews)
                .Select(r => (double)r.Rating)
                .ToList();

            if (!allRatings.Any())
                return null;

            return Math.Round(allRatings.Average(), 2);
        }

        /// <summary>
        /// Tính điểm đánh giá trung bình từ Reviews của 1 sản phẩm
        /// </summary>
        private double? CalculateAverageRating(ICollection<Review>? reviews)
        {
            if (reviews == null || !reviews.Any())
                return null;

            return Math.Round(reviews.Average(r => (double)r.Rating), 2);
        }

        /// <summary>
        /// Tạo URL Google Maps để chỉ đường
        /// </summary>
        private string GenerateDirectionsUrl(double latitude, double longitude)
        {
            return $"https://www.google.com/maps/dir/?api=1&destination={latitude},{longitude}";
        }
    }
}
