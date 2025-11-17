using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Không yêu cầu Authorize ở controller level - cho phép xem sản phẩm công khai
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProductsController(AppDbContext context) => _context = context;

        // 🔥 Thêm helper method này
        private async Task<int?> GetUserIdFromTokenAsync()
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(claimValue))
                return null;

            if (int.TryParse(claimValue, out var userId))
                return userId;

            if (claimValue.Contains("@"))
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == claimValue);
                return user?.Id;
            }

            return null;
        }

        // 🔹 GET: api/products - Cho phép xem công khai (không cần đăng nhập)
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            IQueryable<Product> query = _context.Products
                .Include(p => p.Enterprise)
                .Include(p => p.Category);

            if (role == "EnterpriseAdmin")
            {
                var currentUserId = await GetUserIdFromTokenAsync();
                if (currentUserId == null)
                    return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

                var enterpriseId = await _context.Users
                    .Where(u => u.Id == currentUserId.Value)
                    .Select(u => u.EnterpriseId)
                    .FirstOrDefaultAsync();

                query = query.Where(p => p.EnterpriseId == enterpriseId);
            }
            else if (role == "SystemAdmin")
            {
                // xem tất cả
            }
            else
            {
                query = query.Where(p => p.Status == "Approved");
            }

            var products = await query
                .Include(p => p.Reviews)
                .ToListAsync();

            return Ok(products.Select(MapProductToDto));
        }

        // 🔹 GET: api/products/{id} - Cho phép xem công khai (không cần đăng nhập)
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Reviews)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (product.Status != "Approved")
            {
                if (role == "SystemAdmin")
                {
                    // allow
                }
                else if (role == "EnterpriseAdmin")
                {
                    var currentUserId = await GetUserIdFromTokenAsync();
                    var enterpriseId = currentUserId.HasValue
                        ? await _context.Users
                            .Where(u => u.Id == currentUserId.Value)
                            .Select(u => u.EnterpriseId)
                            .FirstOrDefaultAsync()
                        : null;

                    if (enterpriseId == null || product.EnterpriseId != enterpriseId)
                        return Forbid("Sản phẩm chưa được duyệt.");
                }
                else
                {
                    return NotFound();
                }
            }

            return Ok(MapProductToDto(product));
        }

        // 🔹 POST: api/products
        [Authorize(Roles = "EnterpriseAdmin")]
        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = await GetUserIdFromTokenAsync();
            if (currentUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var enterpriseId = await _context.Users
                .Where(u => u.Id == currentUserId.Value)
                .Select(u => u.EnterpriseId)
                .FirstOrDefaultAsync();

            if (enterpriseId == null)
                return BadRequest("EnterpriseAdmin không thuộc Enterprise nào.");

            Category? category = null;
            if (dto.CategoryId.HasValue)
            {
                category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value && c.IsActive);
                if (category == null)
                    return BadRequest("Danh mục không tồn tại hoặc đã bị vô hiệu hóa.");
            }

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                EnterpriseId = enterpriseId.Value,
                ImageUrl = dto.ImageUrl,
                OCOPRating = dto.OCOPRating,
                StockStatus = dto.StockStatus ?? "InStock",
                CategoryId = dto.CategoryId,
                Status = "PendingApproval",
                ApprovedAt = null,
                ApprovedByUserId = null
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var productDto = MapProductToDto(product);
            productDto.CategoryName = category?.Name;
            productDto.AverageRating = null;

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, productDto);
        }

        // 🔹 PUT: api/products/{id}
        [Authorize(Roles = "EnterpriseAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var currentUserId = await GetUserIdFromTokenAsync();
            if (currentUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var enterpriseId = await _context.Users
                .Where(u => u.Id == currentUserId.Value)
                .Select(u => u.EnterpriseId)
                .FirstOrDefaultAsync();

            if (product.EnterpriseId != enterpriseId)
                return Forbid();

            if (dto.CategoryId.HasValue)
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value && c.IsActive);
                if (category == null)
                    return BadRequest("Danh mục không tồn tại hoặc đã bị vô hiệu hóa.");
            }

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.ImageUrl = dto.ImageUrl;
            product.OCOPRating = dto.OCOPRating;
            product.StockStatus = dto.StockStatus ?? product.StockStatus;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;
            product.Status = "PendingApproval";
            product.ApprovedAt = null;
            product.ApprovedByUserId = null;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 🔹 DELETE: api/products/{id}
        [Authorize(Roles = "EnterpriseAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var currentUserId = await GetUserIdFromTokenAsync();
            if (currentUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var enterpriseId = await _context.Users
                .Where(u => u.Id == currentUserId.Value)
                .Select(u => u.EnterpriseId)
                .FirstOrDefaultAsync();

            if (product.EnterpriseId != enterpriseId)
                return Forbid();

            var hasOrderItems = await _context.OrderItems
                .AnyAsync(oi => oi.ProductId == id);

            if (hasOrderItems)
                return BadRequest("Không thể xóa sản phẩm đã tồn tại trong đơn hàng.");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 🔹 SystemAdmin duyệt / từ chối sản phẩm
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost("{id}/status")]
        public async Task<IActionResult> UpdateProductStatus(int id, [FromBody] UpdateProductStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound("Không tìm thấy sản phẩm.");

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            switch (dto.Status)
            {
                case "Approved":
                    product.Status = "Approved";
                    product.ApprovedAt = DateTime.UtcNow;
                    product.ApprovedByUserId = userId.Value;
                    if (dto.OCOPRating.HasValue)
                        product.OCOPRating = dto.OCOPRating;
                    break;
                case "Rejected":
                    product.Status = "Rejected";
                    product.ApprovedAt = DateTime.UtcNow;
                    product.ApprovedByUserId = userId.Value;
                    break;
                case "PendingApproval":
                    product.Status = "PendingApproval";
                    product.ApprovedAt = null;
                    product.ApprovedByUserId = null;
                    break;
                default:
                    return BadRequest("Trạng thái không hợp lệ. Chỉ chấp nhận: PendingApproval, Approved, Rejected.");
            }

            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static ProductDto MapProductToDto(Product product)
        {
            // 🔹 Sử dụng AverageRating từ database thay vì tính toán động
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                EnterpriseId = product.EnterpriseId,
                ImageUrl = product.ImageUrl,
                OCOPRating = product.OCOPRating,
                StockStatus = product.StockStatus,
                AverageRating = product.AverageRating, // 🔹 Lấy từ database
                Status = product.Status,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                ApprovedAt = product.ApprovedAt,
                ApprovedByUserId = product.ApprovedByUserId
            };
        }
    }

    public class CreateProductDto
    {
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
        [MaxLength(255)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Mô tả sản phẩm là bắt buộc.")]
        [MaxLength(2000)]
        public string Description { get; set; } = "";

        [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn hoặc bằng 0.")]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }
        public int? OCOPRating { get; set; }
        public string? StockStatus { get; set; } // "InStock" or "OutOfStock"
        public int? CategoryId { get; set; }
    }
}
