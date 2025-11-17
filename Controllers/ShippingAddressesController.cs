using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;

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

        // 🔹 Helper: Tạo địa chỉ đầy đủ từ các trường AddressLine, Ward, District, Province
        private string BuildFullAddress(string addressLine, string ward, string district, string province)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(addressLine)) parts.Add(addressLine.Trim());
            if (!string.IsNullOrWhiteSpace(ward)) parts.Add(ward.Trim());
            if (!string.IsNullOrWhiteSpace(district)) parts.Add(district.Trim());
            if (!string.IsNullOrWhiteSpace(province)) parts.Add(province.Trim());
            return string.Join(", ", parts);
        }

        // 🔹 Helper: Map ShippingAddress sang ShippingAddressDto
        private ShippingAddressDto MapToDto(ShippingAddress address)
        {
            return new ShippingAddressDto
            {
                Id = address.Id,
                UserId = address.UserId,
                FullName = address.FullName,
                PhoneNumber = address.PhoneNumber,
                AddressLine = address.AddressLine,
                Ward = address.Ward,
                District = address.District,
                Province = address.Province,
                Address = address.Address, // Full address string
                Label = address.Label,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt
            };
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

            var addressesDto = addresses.Select(MapToDto).ToList();

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

            return Ok(MapToDto(address));
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

            // Tự động tạo địa chỉ đầy đủ từ các trường
            var fullAddress = BuildFullAddress(dto.AddressLine, dto.Ward, dto.District, dto.Province);

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
                FullName = dto.FullName.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                AddressLine = dto.AddressLine.Trim(),
                Ward = dto.Ward.Trim(),
                District = dto.District.Trim(),
                Province = dto.Province.Trim(),
                Address = fullAddress, // Tự động tạo địa chỉ đầy đủ
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

            return CreatedAtAction(nameof(GetShippingAddress), new { id = shippingAddress.Id }, MapToDto(shippingAddress));
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

            // Cập nhật các trường nếu có
            if (dto.FullName != null)
            {
                if (string.IsNullOrWhiteSpace(dto.FullName))
                    return BadRequest("Họ tên người nhận không được để trống.");
                address.FullName = dto.FullName.Trim();
            }

            if (dto.PhoneNumber != null)
            {
                if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                    return BadRequest("Số điện thoại không được để trống.");
                address.PhoneNumber = dto.PhoneNumber.Trim();
            }

            if (dto.AddressLine != null)
            {
                if (string.IsNullOrWhiteSpace(dto.AddressLine))
                    return BadRequest("Địa chỉ chi tiết không được để trống.");
                address.AddressLine = dto.AddressLine.Trim();
            }

            if (dto.Ward != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Ward))
                    return BadRequest("Phường/Xã không được để trống.");
                address.Ward = dto.Ward.Trim();
            }

            if (dto.District != null)
            {
                if (string.IsNullOrWhiteSpace(dto.District))
                    return BadRequest("Quận/Huyện không được để trống.");
                address.District = dto.District.Trim();
            }

            if (dto.Province != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Province))
                    return BadRequest("Tỉnh/Thành phố không được để trống.");
                address.Province = dto.Province.Trim();
            }

            // Cập nhật địa chỉ đầy đủ nếu có thay đổi về địa chỉ
            if (dto.AddressLine != null || dto.Ward != null || dto.District != null || dto.Province != null)
            {
                address.Address = BuildFullAddress(address.AddressLine, address.Ward, address.District, address.Province);
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
                // Không cho phép bỏ mặc định nếu đây là địa chỉ duy nhất
                var otherAddressesExist = await _context.ShippingAddresses
                    .AnyAsync(sa => sa.UserId == userId.Value && sa.Id != id);
                if (!otherAddressesExist)
                {
                    return BadRequest("Không thể bỏ mặc định địa chỉ duy nhất của bạn.");
                }
                address.IsDefault = false;
            }

            address.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(MapToDto(address));
        }

        // 🔹 DELETE: api/shipping-addresses/{id} - Xóa địa chỉ
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShippingAddress(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var address = await _context.ShippingAddresses
                .Include(sa => sa.Orders) // Load Orders để kiểm tra
                .FirstOrDefaultAsync(sa => sa.Id == id && sa.UserId == userId.Value);

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ giao hàng.");

            // 🔹 Kiểm tra xem địa chỉ có đang dùng trong đơn hàng không
            // Nếu có, không cho phép xóa
            var hasOrders = await _context.Orders
                .AnyAsync(o => o.ShippingAddressId == id);

            if (hasOrders)
            {
                return BadRequest("Không thể xóa địa chỉ này vì đang được sử dụng trong đơn hàng. Vui lòng liên hệ quản trị viên nếu cần hỗ trợ.");
            }

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
