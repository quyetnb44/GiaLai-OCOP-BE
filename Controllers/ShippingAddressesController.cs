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
    public class ShippingAddressesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ShippingAddressesController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Helper: Lấy UserId từ JWT token
        private async Task<int?> GetCurrentUserIdAsync()
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(claimValue))
                return null;

            if (int.TryParse(claimValue, out var userId))
                return userId;

            if (claimValue.Contains("@"))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == claimValue);
                return user?.Id;
            }

            return null;
        }

        // 🔹 GET: api/shipping-addresses - Lấy tất cả địa chỉ của user hiện tại
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShippingAddressDto>>> GetShippingAddresses()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var addresses = await _context.ShippingAddresses
                .Where(sa => sa.UserId == userId.Value)
                .OrderByDescending(sa => sa.IsDefault)
                .ThenByDescending(sa => sa.CreatedAt)
                .ToListAsync();

            var addressesDto = addresses.Select(sa => new ShippingAddressDto
            {
                Id = sa.Id,
                UserId = sa.UserId,
                Address = sa.Address,
                Label = sa.Label,
                IsDefault = sa.IsDefault,
                CreatedAt = sa.CreatedAt,
                UpdatedAt = sa.UpdatedAt
            }).ToList();

            return Ok(addressesDto);
        }

        // 🔹 GET: api/shipping-addresses/{id} - Lấy địa chỉ theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<ShippingAddressDto>> GetShippingAddress(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(sa => sa.Id == id && sa.UserId == userId.Value);

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ giao hàng.");

            var addressDto = new ShippingAddressDto
            {
                Id = address.Id,
                UserId = address.UserId,
                Address = address.Address,
                Label = address.Label,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt
            };

            return Ok(addressDto);
        }

        // 🔹 POST: api/shipping-addresses - Tạo địa chỉ mới
        [HttpPost]
        public async Task<ActionResult<ShippingAddressDto>> CreateShippingAddress([FromBody] CreateShippingAddressDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            // Validation
            if (string.IsNullOrWhiteSpace(dto.Address))
                return BadRequest("Địa chỉ giao hàng không được để trống.");

            // Nếu đặt làm mặc định, bỏ mặc định của các địa chỉ khác
            if (dto.IsDefault)
            {
                var existingDefault = await _context.ShippingAddresses
                    .Where(sa => sa.UserId == userId.Value && sa.IsDefault)
                    .ToListAsync();

                foreach (var addr in existingDefault)
                {
                    addr.IsDefault = false;
                }
            }

            var shippingAddress = new ShippingAddress
            {
                UserId = userId.Value,
                Address = dto.Address.Trim(),
                Label = string.IsNullOrWhiteSpace(dto.Label) ? null : dto.Label.Trim(),
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            // Nếu đây là địa chỉ đầu tiên, tự động đặt làm mặc định
            var hasAnyAddress = await _context.ShippingAddresses
                .AnyAsync(sa => sa.UserId == userId.Value);

            if (!hasAnyAddress)
            {
                shippingAddress.IsDefault = true;
            }

            _context.ShippingAddresses.Add(shippingAddress);
            await _context.SaveChangesAsync();

            var addressDto = new ShippingAddressDto
            {
                Id = shippingAddress.Id,
                UserId = shippingAddress.UserId,
                Address = shippingAddress.Address,
                Label = shippingAddress.Label,
                IsDefault = shippingAddress.IsDefault,
                CreatedAt = shippingAddress.CreatedAt,
                UpdatedAt = shippingAddress.UpdatedAt
            };

            return CreatedAtAction(nameof(GetShippingAddress), new { id = shippingAddress.Id }, addressDto);
        }

        // 🔹 PUT: api/shipping-addresses/{id} - Cập nhật địa chỉ
        [HttpPut("{id}")]
        public async Task<ActionResult<ShippingAddressDto>> UpdateShippingAddress(int id, [FromBody] UpdateShippingAddressItemDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(sa => sa.Id == id && sa.UserId == userId.Value);

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ giao hàng.");

            // Cập nhật địa chỉ nếu có
            if (dto.Address != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Address))
                    return BadRequest("Địa chỉ giao hàng không được để trống.");

                address.Address = dto.Address.Trim();
            }

            // Cập nhật label nếu có
            if (dto.Label != null)
            {
                address.Label = string.IsNullOrWhiteSpace(dto.Label) ? null : dto.Label.Trim();
            }

            // Cập nhật IsDefault nếu có
            if (dto.IsDefault.HasValue && dto.IsDefault.Value)
            {
                // Bỏ mặc định của các địa chỉ khác
                var existingDefault = await _context.ShippingAddresses
                    .Where(sa => sa.UserId == userId.Value && sa.IsDefault && sa.Id != id)
                    .ToListAsync();

                foreach (var addr in existingDefault)
                {
                    addr.IsDefault = false;
                }

                address.IsDefault = true;
            }
            else if (dto.IsDefault.HasValue && !dto.IsDefault.Value)
            {
                address.IsDefault = false;
            }

            address.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var addressDto = new ShippingAddressDto
            {
                Id = address.Id,
                UserId = address.UserId,
                Address = address.Address,
                Label = address.Label,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt
            };

            return Ok(addressDto);
        }

        // 🔹 DELETE: api/shipping-addresses/{id} - Xóa địa chỉ
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShippingAddress(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(sa => sa.Id == id && sa.UserId == userId.Value);

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ giao hàng.");

            var wasDefault = address.IsDefault;

            _context.ShippingAddresses.Remove(address);
            await _context.SaveChangesAsync();

            // Nếu xóa địa chỉ mặc định, đặt địa chỉ đầu tiên làm mặc định
            if (wasDefault)
            {
                var firstAddress = await _context.ShippingAddresses
                    .Where(sa => sa.UserId == userId.Value)
                    .OrderBy(sa => sa.CreatedAt)
                    .FirstOrDefaultAsync();

                if (firstAddress != null)
                {
                    firstAddress.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }

            return NoContent();
        }
    }
}

