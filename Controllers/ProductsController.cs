using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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

        // 🔹 GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            IQueryable<Product> query = _context.Products.Include(p => p.Enterprise);

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

            var products = await query
                .Include(p => p.Reviews)
                .ToListAsync();

            var result = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                EnterpriseId = p.EnterpriseId,
                ImageUrl = p.ImageUrl,
                OCOPRating = p.OCOPRating,
                StockStatus = p.StockStatus,
                AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => (double)r.Rating) : null
            });

            return Ok(result);
        }

        // 🔹 GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (product == null) return NotFound();

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                EnterpriseId = product.EnterpriseId,
                ImageUrl = product.ImageUrl,
                OCOPRating = product.OCOPRating,
                StockStatus = product.StockStatus,
                AverageRating = product.Reviews.Any() ? product.Reviews.Average(r => (double)r.Rating) : null
            };

            return Ok(productDto);
        }

        // 🔹 POST: api/products
        [Authorize(Roles = "EnterpriseAdmin")]
        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto dto)
        {
            var currentUserId = await GetUserIdFromTokenAsync();
            if (currentUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
            
            var enterpriseId = await _context.Users
                .Where(u => u.Id == currentUserId.Value)
                .Select(u => u.EnterpriseId)
                .FirstOrDefaultAsync();

            if (enterpriseId == null)
                return BadRequest("EnterpriseAdmin không thuộc Enterprise nào.");

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                EnterpriseId = enterpriseId.Value,
                ImageUrl = dto.ImageUrl,
                OCOPRating = dto.OCOPRating,
                StockStatus = dto.StockStatus ?? "InStock"
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                EnterpriseId = product.EnterpriseId,
                ImageUrl = product.ImageUrl,
                OCOPRating = product.OCOPRating,
                StockStatus = product.StockStatus,
                AverageRating = null
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, productDto);
        }

        // 🔹 PUT: api/products/{id}
        [Authorize(Roles = "EnterpriseAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] CreateProductDto dto)
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

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.ImageUrl = dto.ImageUrl;
            product.OCOPRating = dto.OCOPRating;
            product.StockStatus = dto.StockStatus ?? product.StockStatus;
            product.UpdatedAt = DateTime.UtcNow;

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

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class CreateProductDto
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int? OCOPRating { get; set; }
        public string? StockStatus { get; set; } // "InStock" or "OutOfStock"
    }
}
