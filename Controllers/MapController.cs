using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using System.Linq.Expressions;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MapController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 FR-MAP-01: Tìm kiếm doanh nghiệp OCOP theo từ khóa
        // GET: api/map/search?keyword=...
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<EnterpriseMapDto>>> SearchEnterprises([FromQuery] string? keyword)
        {
            var query = _context.Enterprises
                .Where(e => e.Latitude != null && e.Longitude != null) // Chỉ lấy doanh nghiệp có tọa độ
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

            // Tính điểm đánh giá trung bình
            var enterprisesWithRating = enterprises.Select(e => new EnterpriseMapDto
            {
                Id = e.Id,
                Name = e.Name,
                Address = e.Address,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                ImageUrl = e.ImageUrl,
                AverageRating = CalculateAverageRating(e.Products),
                OCOPRating = e.OCOPRating,
                District = e.District,
                Province = e.Province
            }).ToList();

            return Ok(enterprisesWithRating);
        }

        // 🔹 FR-MAP-02: Tìm doanh nghiệp theo khu vực bản đồ (bounding box)
        // GET: api/map/bounding-box?minLat=...&maxLat=...&minLng=...&maxLng=...
        [HttpGet("bounding-box")]
        public async Task<ActionResult<IEnumerable<EnterpriseMapDto>>> GetEnterprisesByBoundingBox(
            [FromQuery] double minLatitude,
            [FromQuery] double maxLatitude,
            [FromQuery] double minLongitude,
            [FromQuery] double maxLongitude)
        {
            // Validation
            if (minLatitude < -90 || maxLatitude > 90 || minLatitude > maxLatitude)
                return BadRequest("Latitude không hợp lệ.");

            if (minLongitude < -180 || maxLongitude > 180 || minLongitude > maxLongitude)
                return BadRequest("Longitude không hợp lệ.");

            var enterprises = await _context.Enterprises
                .Where(e => e.Latitude != null && e.Longitude != null &&
                           e.Latitude >= minLatitude && e.Latitude <= maxLatitude &&
                           e.Longitude >= minLongitude && e.Longitude <= maxLongitude)
                .Include(e => e.Products!)
                    .ThenInclude(p => p.Reviews)
                .ToListAsync();

            var enterprisesDto = enterprises.Select(e => new EnterpriseMapDto
            {
                Id = e.Id,
                Name = e.Name,
                Address = e.Address,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                ImageUrl = e.ImageUrl,
                AverageRating = CalculateAverageRating(e.Products),
                OCOPRating = e.OCOPRating,
                District = e.District,
                Province = e.Province
            }).ToList();

            return Ok(enterprisesDto);
        }

        // 🔹 FR-MAP-08: API tìm kiếm theo tọa độ và bán kính
        // GET: api/map/nearby?lat=...&lng=...&radius=...
        [HttpGet("nearby")]
        public async Task<ActionResult<IEnumerable<EnterpriseMapDto>>> GetNearbyEnterprises(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radius = 10) // Mặc định 10km
        {
            // Validation
            if (latitude < -90 || latitude > 90)
                return BadRequest("Latitude phải nằm trong khoảng -90 đến 90.");

            if (longitude < -180 || longitude > 180)
                return BadRequest("Longitude phải nằm trong khoảng -180 đến 180.");

            if (radius <= 0 || radius > 100)
                return BadRequest("Radius phải lớn hơn 0 và nhỏ hơn 100 km.");

            // Lấy tất cả doanh nghiệp có tọa độ
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
                .OrderBy(x => x.Distance)
                .Select(x => new EnterpriseMapDto
                {
                    Id = x.Enterprise.Id,
                    Name = x.Enterprise.Name,
                    Address = x.Enterprise.Address,
                    Latitude = x.Enterprise.Latitude,
                    Longitude = x.Enterprise.Longitude,
                    ImageUrl = x.Enterprise.ImageUrl,
                    AverageRating = CalculateAverageRating(x.Enterprise.Products),
                    OCOPRating = x.Enterprise.OCOPRating,
                    District = x.Enterprise.District,
                    Province = x.Enterprise.Province
                })
                .ToList();

            return Ok(nearbyEnterprises);
        }

        // 🔹 FR-MAP-06: Lọc doanh nghiệp theo điều kiện
        // GET: api/map/filter?district=...&ocopRating=...&businessField=...&lat=...&lng=...&maxDistance=...
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<EnterpriseMapDto>>> FilterEnterprises([FromQuery] MapSearchRequestDto request)
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

            var enterprisesDto = enterprises.Select(e => new EnterpriseMapDto
            {
                Id = e.Id,
                Name = e.Name,
                Address = e.Address,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                ImageUrl = e.ImageUrl,
                AverageRating = CalculateAverageRating(e.Products),
                OCOPRating = e.OCOPRating,
                District = e.District,
                Province = e.Province
            }).ToList();

            // Phân trang
            var totalCount = enterprisesDto.Count;
            var pagedResults = enterprisesDto
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return Ok(new
            {
                Total = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                Data = pagedResults
            });
        }

        // 🔹 FR-MAP-04: Popup chi tiết doanh nghiệp khi click marker
        // GET: api/map/enterprises/{id}
        [HttpGet("enterprises/{id}")]
        public async Task<ActionResult<EnterpriseDetailDto>> GetEnterpriseDetail(int id)
        {
            var enterprise = await _context.Enterprises
                .Include(e => e.Products!)
                    .ThenInclude(p => p.Reviews)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enterprise == null)
                return NotFound("Không tìm thấy doanh nghiệp.");

            // Lấy 3 sản phẩm nổi bật (có OCOPRating cao nhất)
            var featuredProducts = enterprise.Products?
                .OrderByDescending(p => p.OCOPRating ?? 0)
                .ThenByDescending(p => CalculateAverageRating(p.Reviews))
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
                TotalProducts = enterprise.Products?.Count ?? 0
            };

            return Ok(enterpriseDetail);
        }

        // 🔹 FR-MAP-05: Hiển thị danh sách sản phẩm khi chọn doanh nghiệp
        // GET: api/map/enterprises/{id}/products
        [HttpGet("enterprises/{id}/products")]
        public async Task<ActionResult<IEnumerable<ProductMapDto>>> GetEnterpriseProducts(int id)
        {
            var enterprise = await _context.Enterprises
                .Include(e => e.Products!)
                    .ThenInclude(p => p.Reviews)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enterprise == null)
                return NotFound("Không tìm thấy doanh nghiệp.");

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

            return Ok(products);
        }

        // 🔹 Helper: Tính khoảng cách giữa 2 điểm (Haversine formula)
        // Trả về khoảng cách tính bằng km
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Bán kính Trái Đất (km)
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        // 🔹 Helper: Tính điểm đánh giá trung bình từ Reviews
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

            return allRatings.Average();
        }

        // 🔹 Overload: Tính điểm đánh giá từ Reviews của 1 sản phẩm
        private double? CalculateAverageRating(ICollection<Review>? reviews)
        {
            if (reviews == null || !reviews.Any())
                return null;

            return reviews.Average(r => (double)r.Rating);
        }
    }
}

