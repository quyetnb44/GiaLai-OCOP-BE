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
    public class EnterprisesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public EnterprisesController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Helper: Lấy userId từ token
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

        // 🔹 Helper: Map Enterprise to DTO
        private EnterpriseDto MapEnterpriseToDto(Enterprise enterprise)
        {
            return new EnterpriseDto
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
                OCOPRating = enterprise.OCOPRating,
                BusinessField = enterprise.BusinessField,
                ImageUrl = enterprise.ImageUrl,
                AverageRating = enterprise.AverageRating,
                Products = (enterprise.Products ?? new List<Product>()).Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    EnterpriseId = enterprise.Id,
                    ImageUrl = p.ImageUrl,
                    OCOPRating = p.OCOPRating,
                    StockStatus = p.StockStatus,
                    AverageRating = p.AverageRating,
                    Status = p.Status,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name,
                    ApprovedAt = p.ApprovedAt,
                    ApprovedByUserId = p.ApprovedByUserId
                }).ToList(),
                Users = (enterprise.Users ?? new List<User>()).Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role,
                    EnterpriseId = u.EnterpriseId,
                    IsEmailVerified = u.IsEmailVerified,
                    PhoneNumber = u.PhoneNumber,
                    Gender = u.Gender,
                    DateOfBirth = u.DateOfBirth,
                    ShippingAddress = u.ShippingAddress,
                    AvatarUrl = u.AvatarUrl,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                }).ToList()
            };
        }

        // 🔹 GET: api/enterprises/me - EnterpriseAdmin xem doanh nghiệp của mình
        [HttpGet("me")]
        [Authorize(Roles = "EnterpriseAdmin")]
        public async Task<ActionResult<EnterpriseDto>> GetMyEnterprise()
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var user = await _context.Users
                .Include(u => u.Enterprise)
                    .ThenInclude(e => e!.Products)!
                        .ThenInclude(p => p.Category)
                .Include(u => u.Enterprise)
                    .ThenInclude(e => e!.Users)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound("Không tìm thấy người dùng.");

            if (user.EnterpriseId == null || user.Enterprise == null)
                return NotFound("Bạn không thuộc doanh nghiệp nào.");

            return Ok(MapEnterpriseToDto(user.Enterprise));
        }

        // 🔹 PUT: api/enterprises/me - EnterpriseAdmin cập nhật thông tin doanh nghiệp của mình
        [HttpPut("me")]
        [Authorize(Roles = "EnterpriseAdmin")]
        public async Task<IActionResult> UpdateMyEnterprise([FromBody] UpdateEnterpriseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var user = await _context.Users
                .Include(u => u.Enterprise)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound("Không tìm thấy người dùng.");

            if (user.EnterpriseId == null || user.Enterprise == null)
                return NotFound("Bạn không thuộc doanh nghiệp nào.");

            var enterprise = user.Enterprise;

            // 🔹 Cập nhật các trường được phép (không bao gồm OCOPRating)
            enterprise.Name = dto.Name;
            if (dto.Description != null)
                enterprise.Description = dto.Description;
            if (dto.Address != null)
                enterprise.Address = dto.Address;
            if (dto.Ward != null)
                enterprise.Ward = dto.Ward;
            if (dto.District != null)
                enterprise.District = dto.District;
            if (dto.Province != null)
                enterprise.Province = dto.Province;
            if (dto.Latitude.HasValue)
                enterprise.Latitude = dto.Latitude;
            if (dto.Longitude.HasValue)
                enterprise.Longitude = dto.Longitude;
            if (dto.PhoneNumber != null)
                enterprise.PhoneNumber = dto.PhoneNumber;
            if (dto.EmailContact != null)
                enterprise.EmailContact = dto.EmailContact;
            if (dto.Website != null)
                enterprise.Website = dto.Website;
            if (dto.BusinessField != null)
                enterprise.BusinessField = dto.BusinessField;
            if (dto.ImageUrl != null)
                enterprise.ImageUrl = dto.ImageUrl;

            enterprise.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/Enterprises - Chỉ SystemAdmin
        [HttpGet]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<ActionResult<IEnumerable<EnterpriseDto>>> GetEnterprises()
        {
            var enterprises = await _context.Enterprises
                                            .Include(e => e.Products)!
                                                .ThenInclude(p => p.Category)
                                            .Include(e => e.Users)
                                            .ToListAsync();

            var enterpriseDtos = enterprises.Select(e => MapEnterpriseToDto(e)).ToList();

            return Ok(enterpriseDtos);
        }

        // GET: api/Enterprises/5 - Chỉ SystemAdmin
        [HttpGet("{id}")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<ActionResult<EnterpriseDto>> GetEnterprise(int id)
        {
            var enterprise = await _context.Enterprises
                                           .Include(e => e.Products)!
                                                .ThenInclude(p => p.Category)
                                           .Include(e => e.Users)
                                           .FirstOrDefaultAsync(e => e.Id == id);

            if (enterprise == null) return NotFound();

            return Ok(MapEnterpriseToDto(enterprise));
        }

        // POST: api/Enterprises - Chỉ SystemAdmin
        [HttpPost]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<ActionResult<EnterpriseDto>> CreateEnterprise([FromBody] Enterprise enterprise)
        {
            _context.Enterprises.Add(enterprise);
            await _context.SaveChangesAsync();

            var enterpriseDto = new EnterpriseDto
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
                OCOPRating = enterprise.OCOPRating,
                BusinessField = enterprise.BusinessField,
                ImageUrl = enterprise.ImageUrl,
                AverageRating = enterprise.AverageRating,
                Products = new List<ProductDto>(),
                Users = new List<UserDto>()
            };

            return CreatedAtAction(nameof(GetEnterprise), new { id = enterprise.Id }, enterpriseDto);
        }

        // PUT: api/Enterprises/5 - Chỉ SystemAdmin
        [HttpPut("{id}")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> UpdateEnterprise(int id, [FromBody] Enterprise enterprise)
        {
            if (id != enterprise.Id) return BadRequest();

            _context.Entry(enterprise).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Enterprises/5 - Chỉ SystemAdmin
        [HttpDelete("{id}")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> DeleteEnterprise(int id)
        {
            var enterprise = await _context.Enterprises.FindAsync(id);
            if (enterprise == null) return NotFound();

            _context.Enterprises.Remove(enterprise);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
