using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;

namespace GiaLaiOCOP.Api.Controllers
{
    [ApiController]
    [Route("api/shipping-addresses")]
    [Authorize] // Tất cả endpoints đều yêu cầu đăng nhập
    public class ShippingAddressesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IGpsAddressService _gpsAddressService;

        public ShippingAddressesController(AppDbContext context, IGpsAddressService gpsAddressService)
        {
            _context = context;
            _gpsAddressService = gpsAddressService;
        }

        // 🔹 Helper: Lấy UserId từ token
        private async Task<int?> GetCurrentUserIdAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            var emailClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                             ?? User.FindFirst(ClaimTypes.Email)?.Value;

            if (!string.IsNullOrWhiteSpace(emailClaim))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);
                if (user != null)
                {
                    return user.Id;
                }
            }

            return null;
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
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt
            };
        }

        // 🔹 Helper: Map DTO sang model (tạo mới)
        private ShippingAddress MapToModel(CreateShippingAddressDto dto, int userId)
        {
            return new ShippingAddress
            {
                UserId = userId,
                FullName = dto.FullName.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                AddressLine = dto.AddressLine.Trim(),
                Ward = dto.Ward.Trim(),
                District = dto.District.Trim(),
                Province = dto.Province.Trim(),
                Label = string.IsNullOrWhiteSpace(dto.Label) ? null : dto.Label.Trim(),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow
            };
        }

        // 🔹 Helper: Map DTO sang model (cập nhật)
        private void MapToModel(UpdateShippingAddressItemDto dto, ShippingAddress address)
        {
            address.FullName = dto.FullName.Trim();
            address.PhoneNumber = dto.PhoneNumber.Trim();
            address.AddressLine = dto.AddressLine.Trim();
            address.Ward = dto.Ward.Trim();
            address.District = dto.District.Trim();
            address.Province = dto.Province.Trim();
            address.Label = string.IsNullOrWhiteSpace(dto.Label) ? null : dto.Label.Trim();
            address.Latitude = dto.Latitude;
            address.Longitude = dto.Longitude;
            address.IsDefault = dto.IsDefault;
            address.UpdatedAt = DateTime.UtcNow;
        }

        // 🔹 GET: api/shippingaddress/from-gps - lấy địa chỉ từ GPS
        [AllowAnonymous]
        [HttpGet("/api/shippingaddress/from-gps")]
        public async Task<ActionResult<GpsAddressLookupDto>> GetAddressFromGps([FromQuery] double? lat, [FromQuery] double? lng, CancellationToken cancellationToken)
        {
            if (!lat.HasValue || !lng.HasValue)
            {
                return BadRequest("Vui lòng cung cấp đầy đủ lat và lng.");
            }

            if (lat is < -90 or > 90 || lng is < -180 or > 180)
            {
                return BadRequest("Tọa độ không hợp lệ. Vĩ độ (-90 đến 90), kinh độ (-180 đến 180).");
            }

            try
            {
                var result = await _gpsAddressService.GetAddressFromGpsAsync(lat.Value, lng.Value, cancellationToken);
                if (result == null)
                {
                    return BadRequest("Không tìm thấy địa chỉ phù hợp với tọa độ đã cung cấp.");
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException)
            {
                return BadRequest("Không thể kết nối tới dịch vụ bản đồ. Vui lòng thử lại sau.");
            }
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

            var shippingAddress = MapToModel(dto, currentUserId.Value);

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
            MapToModel(dto, address);

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

