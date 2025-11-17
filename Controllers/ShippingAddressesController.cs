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
    [Route("api/shipping-addresses")]
    [ApiController]
    [Authorize] // Tất cả endpoints yêu cầu đăng nhập
    public class ShippingAddressesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ShippingAddressesController(AppDbContext context)
        {
            _context = context;
        }

        // Helper method để lấy userId từ JWT token
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
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == claimValue);
                return user?.Id;
            }

            return null;
        }

        // GET: api/shipping-addresses - Lấy tất cả địa chỉ của user hiện tại
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShippingAddressDto>>> GetShippingAddresses()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            // 🔒 SECURITY: Chỉ lấy địa chỉ của user hiện tại
            var addresses = await _context.ShippingAddresses
                .Where(sa => sa.UserId == userId.Value)
                .OrderByDescending(sa => sa.IsDefault)
                .ThenByDescending(sa => sa.CreatedAt)
                .Select(sa => new ShippingAddressDto
                {
                    Id = sa.Id,
                    UserId = sa.UserId,
                    FullName = sa.FullName,
                    PhoneNumber = sa.PhoneNumber,
                    AddressLine = sa.AddressLine,
                    Ward = sa.Ward,
                    District = sa.District,
                    Province = sa.Province,
                    Label = sa.Label,
                    IsDefault = sa.IsDefault,
                    CreatedAt = sa.CreatedAt,
                    UpdatedAt = sa.UpdatedAt
                })
                .ToListAsync();

            return Ok(addresses);
        }

        // GET: api/shipping-addresses/{id} - Lấy một địa chỉ cụ thể
        [HttpGet("{id}")]
        public async Task<ActionResult<ShippingAddressDto>> GetShippingAddress(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var address = await _context.ShippingAddresses
                .Where(sa => sa.Id == id && sa.UserId == userId.Value) // 🔒 SECURITY: Chỉ lấy địa chỉ của user hiện tại
                .Select(sa => new ShippingAddressDto
                {
                    Id = sa.Id,
                    UserId = sa.UserId,
                    FullName = sa.FullName,
                    PhoneNumber = sa.PhoneNumber,
                    AddressLine = sa.AddressLine,
                    Ward = sa.Ward,
                    District = sa.District,
                    Province = sa.Province,
                    Label = sa.Label,
                    IsDefault = sa.IsDefault,
                    CreatedAt = sa.CreatedAt,
                    UpdatedAt = sa.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ giao hàng.");

            return Ok(address);
        }

        // POST: api/shipping-addresses - Tạo địa chỉ mới
        [HttpPost]
        public async Task<ActionResult<ShippingAddressDto>> CreateShippingAddress(CreateShippingAddressDto dto)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            // 🔒 SECURITY: Luôn set UserId từ token, KHÔNG từ client
            var shippingAddress = new ShippingAddress
            {
                UserId = userId.Value, // 🔒 SECURITY: Lấy từ token, không từ client
                FullName = dto.FullName.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                AddressLine = dto.AddressLine.Trim(),
                Ward = dto.Ward.Trim(),
                District = dto.District.Trim(),
                Province = dto.Province.Trim(),
                Label = string.IsNullOrWhiteSpace(dto.Label) ? null : dto.Label.Trim(),
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

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
            // Nếu chưa có địa chỉ nào, tự động đặt làm mặc định
            else
            {
                var hasAnyAddress = await _context.ShippingAddresses
                    .AnyAsync(sa => sa.UserId == userId.Value);

                if (!hasAnyAddress)
                {
                    shippingAddress.IsDefault = true;
                }
            }

            _context.ShippingAddresses.Add(shippingAddress);
            await _context.SaveChangesAsync();

            var result = new ShippingAddressDto
            {
                Id = shippingAddress.Id,
                UserId = shippingAddress.UserId,
                FullName = shippingAddress.FullName,
                PhoneNumber = shippingAddress.PhoneNumber,
                AddressLine = shippingAddress.AddressLine,
                Ward = shippingAddress.Ward,
                District = shippingAddress.District,
                Province = shippingAddress.Province,
                Label = shippingAddress.Label,
                IsDefault = shippingAddress.IsDefault,
                CreatedAt = shippingAddress.CreatedAt,
                UpdatedAt = shippingAddress.UpdatedAt
            };

            return CreatedAtAction(nameof(GetShippingAddress), new { id = shippingAddress.Id }, result);
        }

        // PUT: api/shipping-addresses/{id} - Cập nhật địa chỉ
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShippingAddress(int id, UpdateShippingAddressItemDto dto)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var address = await _context.ShippingAddresses
                .Where(sa => sa.Id == id && sa.UserId == userId.Value) // 🔒 SECURITY: Chỉ cập nhật địa chỉ của user hiện tại
                .FirstOrDefaultAsync();

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ giao hàng.");

            // Kiểm tra xem địa chỉ có đang được sử dụng trong order không
            var isUsedInOrder = await _context.Orders
                .AnyAsync(o => o.ShippingAddressId == id);

            if (isUsedInOrder)
                return BadRequest("Không thể cập nhật địa chỉ đang được sử dụng trong đơn hàng.");

            // Cập nhật thông tin
            address.FullName = dto.FullName.Trim();
            address.PhoneNumber = dto.PhoneNumber.Trim();
            address.AddressLine = dto.AddressLine.Trim();
            address.Ward = dto.Ward.Trim();
            address.District = dto.District.Trim();
            address.Province = dto.Province.Trim();
            address.Label = string.IsNullOrWhiteSpace(dto.Label) ? null : dto.Label.Trim();
            address.UpdatedAt = DateTime.UtcNow;

            // Nếu đặt làm mặc định, bỏ mặc định của các địa chỉ khác
            if (dto.IsDefault && !address.IsDefault)
            {
                var existingDefault = await _context.ShippingAddresses
                    .Where(sa => sa.UserId == userId.Value && sa.IsDefault && sa.Id != id)
                    .ToListAsync();

                foreach (var addr in existingDefault)
                {
                    addr.IsDefault = false;
                }
                address.IsDefault = true;
            }
            else if (!dto.IsDefault && address.IsDefault)
            {
                // Nếu bỏ mặc định, đặt địa chỉ đầu tiên khác làm mặc định
                var firstOther = await _context.ShippingAddresses
                    .Where(sa => sa.UserId == userId.Value && sa.Id != id)
                    .OrderBy(sa => sa.CreatedAt)
                    .FirstOrDefaultAsync();

                if (firstOther != null)
                {
                    firstOther.IsDefault = true;
                }
                address.IsDefault = false;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/shipping-addresses/{id} - Xóa địa chỉ
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShippingAddress(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var address = await _context.ShippingAddresses
                .Where(sa => sa.Id == id && sa.UserId == userId.Value) // 🔒 SECURITY: Chỉ xóa địa chỉ của user hiện tại
                .FirstOrDefaultAsync();

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ giao hàng.");

            // Kiểm tra xem địa chỉ có đang được sử dụng trong order không
            var isUsedInOrder = await _context.Orders
                .AnyAsync(o => o.ShippingAddressId == id);

            if (isUsedInOrder)
                return BadRequest("Không thể xóa địa chỉ đang được sử dụng trong đơn hàng.");

            var wasDefault = address.IsDefault;

            _context.ShippingAddresses.Remove(address);

            // Nếu xóa địa chỉ mặc định, đặt địa chỉ đầu tiên khác làm mặc định
            if (wasDefault)
            {
                var firstOther = await _context.ShippingAddresses
                    .Where(sa => sa.UserId == userId.Value)
                    .OrderBy(sa => sa.CreatedAt)
                    .FirstOrDefaultAsync();

                if (firstOther != null)
                {
                    firstOther.IsDefault = true;
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/shipping-addresses/{id}/set-default - Đặt địa chỉ làm mặc định
        [HttpPut("{id}/set-default")]
        public async Task<IActionResult> SetDefaultShippingAddress(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var address = await _context.ShippingAddresses
                .Where(sa => sa.Id == id && sa.UserId == userId.Value) // 🔒 SECURITY: Chỉ đặt mặc định địa chỉ của user hiện tại
                .FirstOrDefaultAsync();

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ giao hàng.");

            // Bỏ mặc định của các địa chỉ khác
            var existingDefault = await _context.ShippingAddresses
                .Where(sa => sa.UserId == userId.Value && sa.IsDefault && sa.Id != id)
                .ToListAsync();

            foreach (var addr in existingDefault)
            {
                addr.IsDefault = false;
            }

            address.IsDefault = true;
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
