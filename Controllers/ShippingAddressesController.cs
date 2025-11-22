using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using System.Security.Claims;

namespace GiaLaiOCOP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Tất cả endpoints đều yêu cầu đăng nhập
    public class ShippingAddressesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ShippingAddressesController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Helper: Lấy UserId từ token
        private Task<int?> GetCurrentUserIdAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                return Task.FromResult<int?>(userId);
            }
            return Task.FromResult<int?>(null);
        }

        // 🔹 Helper: Map ShippingAddress to DTO
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
                Label = address.Label,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt
            };
        }

        // 🔹 GET: api/shipping-addresses - Lấy danh sách địa chỉ của user hiện tại
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShippingAddressDto>>> GetMyShippingAddresses()
        {
            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var addresses = await _context.ShippingAddresses
                .Where(sa => sa.UserId == currentUserId.Value)
                .OrderByDescending(sa => sa.IsDefault) // Địa chỉ mặc định lên đầu
                .ThenByDescending(sa => sa.CreatedAt)
                .ToListAsync();

            var addressesDto = addresses.Select(MapToDto).ToList();
            return Ok(addressesDto);
        }

        // 🔹 GET: api/shipping-addresses/{id} - Lấy chi tiết một địa chỉ
        [HttpGet("{id}")]
        public async Task<ActionResult<ShippingAddressDto>> GetShippingAddress(int id)
        {
            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(sa => sa.Id == id && sa.UserId == currentUserId.Value);

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ hoặc bạn không có quyền truy cập.");

            return Ok(MapToDto(address));
        }

        // 🔹 POST: api/shipping-addresses - Tạo địa chỉ mới
        [HttpPost]
        public async Task<ActionResult<ShippingAddressDto>> CreateShippingAddress([FromBody] CreateShippingAddressDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            // 🔒 Kiểm tra user có tồn tại không
            var user = await _context.Users.FindAsync(currentUserId.Value);
            if (user == null)
                return NotFound("Không tìm thấy người dùng.");

            // 🔒 Kiểm tra trùng địa chỉ (cùng user, cùng địa chỉ chi tiết)
            var duplicateAddress = await _context.ShippingAddresses
                .FirstOrDefaultAsync(sa => 
                    sa.UserId == currentUserId.Value &&
                    sa.AddressLine.Trim().ToLower() == dto.AddressLine.Trim().ToLower() &&
                    sa.Ward.Trim().ToLower() == dto.Ward.Trim().ToLower() &&
                    sa.District.Trim().ToLower() == dto.District.Trim().ToLower() &&
                    sa.Province.Trim().ToLower() == dto.Province.Trim().ToLower() &&
                    sa.PhoneNumber == dto.PhoneNumber);

            if (duplicateAddress != null)
                return Conflict("Địa chỉ này đã tồn tại trong danh sách của bạn.");

            // 🔒 Nếu đặt làm mặc định, bỏ mặc định của các địa chỉ khác
            if (dto.IsDefault)
            {
                var existingDefault = await _context.ShippingAddresses
                    .Where(sa => sa.UserId == currentUserId.Value && sa.IsDefault)
                    .ToListAsync();

                foreach (var addr in existingDefault)
                {
                    addr.IsDefault = false;
                }
            }

            var shippingAddress = new ShippingAddress
            {
                UserId = currentUserId.Value,
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

            _context.ShippingAddresses.Add(shippingAddress);
            await _context.SaveChangesAsync();

            var addressDto = MapToDto(shippingAddress);
            return CreatedAtAction(nameof(GetShippingAddress), new { id = shippingAddress.Id }, addressDto);
        }

        // 🔹 PUT: api/shipping-addresses/{id} - Cập nhật địa chỉ
        [HttpPut("{id}")]
        public async Task<ActionResult<ShippingAddressDto>> UpdateShippingAddress(int id, [FromBody] UpdateShippingAddressItemDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(sa => sa.Id == id && sa.UserId == currentUserId.Value);

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ hoặc bạn không có quyền chỉnh sửa.");

            // 🔒 Kiểm tra trùng địa chỉ (trừ chính địa chỉ đang sửa)
            var duplicateAddress = await _context.ShippingAddresses
                .FirstOrDefaultAsync(sa => 
                    sa.Id != id &&
                    sa.UserId == currentUserId.Value &&
                    sa.AddressLine.Trim().ToLower() == dto.AddressLine.Trim().ToLower() &&
                    sa.Ward.Trim().ToLower() == dto.Ward.Trim().ToLower() &&
                    sa.District.Trim().ToLower() == dto.District.Trim().ToLower() &&
                    sa.Province.Trim().ToLower() == dto.Province.Trim().ToLower() &&
                    sa.PhoneNumber == dto.PhoneNumber);

            if (duplicateAddress != null)
                return Conflict("Địa chỉ này đã tồn tại trong danh sách của bạn.");

            // 🔒 Nếu đặt làm mặc định, bỏ mặc định của các địa chỉ khác
            if (dto.IsDefault && !address.IsDefault)
            {
                var existingDefault = await _context.ShippingAddresses
                    .Where(sa => sa.UserId == currentUserId.Value && sa.IsDefault && sa.Id != id)
                    .ToListAsync();

                foreach (var addr in existingDefault)
                {
                    addr.IsDefault = false;
                }
            }

            // Cập nhật thông tin
            address.FullName = dto.FullName.Trim();
            address.PhoneNumber = dto.PhoneNumber.Trim();
            address.AddressLine = dto.AddressLine.Trim();
            address.Ward = dto.Ward.Trim();
            address.District = dto.District.Trim();
            address.Province = dto.Province.Trim();
            address.Label = string.IsNullOrWhiteSpace(dto.Label) ? null : dto.Label.Trim();
            address.IsDefault = dto.IsDefault;
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(MapToDto(address));
        }

        // 🔹 PATCH: api/shipping-addresses/{id}/set-default - Đặt địa chỉ làm mặc định
        [HttpPatch("{id}/set-default")]
        public async Task<ActionResult<ShippingAddressDto>> SetDefaultAddress(int id)
        {
            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(sa => sa.Id == id && sa.UserId == currentUserId.Value);

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ hoặc bạn không có quyền truy cập.");

            // Bỏ mặc định của tất cả địa chỉ khác
            var existingDefault = await _context.ShippingAddresses
                .Where(sa => sa.UserId == currentUserId.Value && sa.IsDefault && sa.Id != id)
                .ToListAsync();

            foreach (var addr in existingDefault)
            {
                addr.IsDefault = false;
            }

            // Đặt địa chỉ này làm mặc định
            address.IsDefault = true;
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(MapToDto(address));
        }

        // 🔹 DELETE: api/shipping-addresses/{id} - Xóa địa chỉ
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShippingAddress(int id)
        {
            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(sa => sa.Id == id && sa.UserId == currentUserId.Value);

            if (address == null)
                return NotFound("Không tìm thấy địa chỉ hoặc bạn không có quyền xóa.");

            // 🔒 Kiểm tra xem địa chỉ có đang được sử dụng trong đơn hàng không
            var hasOrders = await _context.Orders
                .AnyAsync(o => o.ShippingAddressId == id);

            if (hasOrders)
                return BadRequest("Không thể xóa địa chỉ này vì đã được sử dụng trong đơn hàng.");

            _context.ShippingAddresses.Remove(address);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

