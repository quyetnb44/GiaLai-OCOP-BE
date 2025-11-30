using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UsersController(AppDbContext context) => _context = context;

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
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == claimValue);
                return currentUser?.Id;
            }

            return null;
        }

        private static UserDto MapUserToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                EnterpriseId = user.EnterpriseId,
                Enterprise = user.Enterprise == null ? null : new EnterpriseDto
                {
                    Id = user.Enterprise.Id,
                    Name = user.Enterprise.Name,
                    Description = user.Enterprise.Description
                },
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                ShippingAddress = user.ShippingAddress,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                ProvinceId = user.ProvinceId,
                DistrictId = user.DistrictId,
                WardId = user.WardId,
                AddressDetail = user.AddressDetail
            };
        }

        // 🔹 GET: api/users
        // Chỉ SystemAdmin xem tất cả user
        [Authorize(Roles = "SystemAdmin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Enterprise)
                .ToListAsync();

            var usersDto = users.Select(MapUserToDto).ToList();

            return Ok(usersDto);
        }

        // 🔹 GET: api/users/me - Lấy thông tin user hiện tại
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId == null) return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var user = await _context.Users
                .Include(u => u.Enterprise)
                .FirstOrDefaultAsync(u => u.Id == currentUserId.Value);

            if (user == null) return NotFound();

            return Ok(MapUserToDto(user));
        }

        // 🔹 GET: api/users/{id}
        // SystemAdmin xem tất cả, Customer/EnterpriseAdmin xem chính mình
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var targetUser = await _context.Users.Include(u => u.Enterprise)
                                           .FirstOrDefaultAsync(u => u.Id == id);
            if (targetUser == null) return NotFound();

            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId == null) return Forbid();

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "SystemAdmin" && currentUserId.Value != id)
                return Forbid();

            return Ok(MapUserToDto(targetUser));
        }

        // 🔹 POST: api/users/enterprise-admin
        // Chỉ SystemAdmin tạo EnterpriseAdmin
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost("enterprise-admin")]
        public async Task<ActionResult<UserDto>> CreateEnterpriseAdmin([FromBody] CreateEnterpriseAdminDto dto)
        {
            // 🔹 Kiểm tra validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return Conflict("Email đã được sử dụng.");

            var enterprise = await _context.Enterprises.FindAsync(dto.EnterpriseId);
            if (enterprise == null)
                return BadRequest("EnterpriseId không hợp lệ.");

            var user = new User
            {
                Name = dto.Name.Trim(),
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "EnterpriseAdmin",
                EnterpriseId = dto.EnterpriseId
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            user.Enterprise = enterprise;
            var userDto = MapUserToDto(user);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
        }

        // 🔹 POST: api/users/customer
        // Mọi người tự đăng ký Customer
        [AllowAnonymous]
        [HttpPost("customer")]
        public async Task<ActionResult<UserDto>> CreateCustomer([FromBody] RegisterDto dto)
        {
            // 🔹 Kiểm tra validation
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return Conflict("Email đã được sử dụng.");

            var user = new User
            {
                Name = dto.Name.Trim(),
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Customer"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userDto = MapUserToDto(user);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
        }

        // 🔹 PUT: api/users/me - User tự cập nhật profile
        [HttpPut("me")]
        public async Task<ActionResult<UserDto>> UpdateCurrentUser([FromBody] UpdateUserProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var user = await _context.Users
                .Include(u => u.Enterprise)
                .FirstOrDefaultAsync(u => u.Id == currentUserId.Value);

            if (user == null)
                return NotFound();

            var hasChanges = false;

            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name.Trim() != user.Name)
            {
                user.Name = dto.Name.Trim();
                hasChanges = true;
            }

            if (dto.Email != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Email))
                    return BadRequest("Email không hợp lệ.");

                var newEmail = dto.Email.Trim().ToLower();
                if (!newEmail.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _context.Users.AnyAsync(u => u.Email == newEmail && u.Id != user.Id))
                        return Conflict("Email đã được sử dụng bởi người dùng khác.");

                    user.Email = newEmail;
                    hasChanges = true;
                }
            }

            if (dto.PhoneNumber != null)
            {
                var normalizedPhone = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
                if (normalizedPhone != user.PhoneNumber)
                {
                    user.PhoneNumber = normalizedPhone;
                    hasChanges = true;
                }
            }

            if (dto.Gender != null)
            {
                var normalizedGender = string.IsNullOrWhiteSpace(dto.Gender) ? null : dto.Gender.Trim();
                if (normalizedGender != user.Gender)
                {
                    user.Gender = normalizedGender;
                    hasChanges = true;
                }
            }

            if (dto.DateOfBirth.HasValue && dto.DateOfBirth != user.DateOfBirth)
            {
                user.DateOfBirth = dto.DateOfBirth;
                hasChanges = true;
            }

            if (dto.ShippingAddress != null)
            {
                var normalizedAddress = string.IsNullOrWhiteSpace(dto.ShippingAddress) ? null : dto.ShippingAddress.Trim();
                if (normalizedAddress != user.ShippingAddress)
                {
                    user.ShippingAddress = normalizedAddress;
                    hasChanges = true;
                }
            }

            if (dto.AvatarUrl != null)
            {
                var normalizedAvatar = string.IsNullOrWhiteSpace(dto.AvatarUrl) ? null : dto.AvatarUrl.Trim();
                if (normalizedAvatar != user.AvatarUrl)
                {
                    user.AvatarUrl = normalizedAvatar;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(MapUserToDto(user));
        }

        // 🔹 PUT: api/users/update-shipping-address - Cập nhật địa chỉ giao hàng chi tiết
        [HttpPut("update-shipping-address")]
        public async Task<ActionResult<UserDto>> UpdateShippingAddress([FromBody] UpdateShippingAddressDetailDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var user = await _context.Users
                .Include(u => u.Enterprise)
                .Include(u => u.Province)
                .Include(u => u.District)
                .Include(u => u.Ward)
                .FirstOrDefaultAsync(u => u.Id == currentUserId.Value);

            if (user == null)
                return NotFound("Không tìm thấy người dùng.");

            // Validate ProvinceId
            var province = await _context.Provinces.FindAsync(dto.ProvinceId);
            if (province == null)
                return BadRequest($"Không tìm thấy tỉnh/thành phố với Id = {dto.ProvinceId}.");

            // Validate DistrictId
            var district = await _context.Districts
                .FirstOrDefaultAsync(d => d.Id == dto.DistrictId && d.ProvinceId == dto.ProvinceId);
            if (district == null)
                return BadRequest($"Không tìm thấy quận/huyện với Id = {dto.DistrictId} thuộc tỉnh {province.Name}.");

            // Validate WardId
            var ward = await _context.Wards
                .FirstOrDefaultAsync(w => w.Id == dto.WardId && w.DistrictId == dto.DistrictId);
            if (ward == null)
                return BadRequest($"Không tìm thấy phường/xã với Id = {dto.WardId} thuộc quận/huyện {district.Name}.");

            // Cập nhật địa chỉ
            user.ProvinceId = dto.ProvinceId;
            user.DistrictId = dto.DistrictId;
            user.WardId = dto.WardId;
            user.AddressDetail = dto.AddressDetail.Trim();

            // Cập nhật ShippingAddress để tương thích với code cũ (tạo địa chỉ đầy đủ)
            var fullAddress = $"{dto.AddressDetail.Trim()}, {ward.Name}, {district.Name}, {province.Name}";
            user.ShippingAddress = fullAddress;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload để lấy dữ liệu mới nhất
            user = await _context.Users
                .Include(u => u.Enterprise)
                .Include(u => u.Province)
                .Include(u => u.District)
                .Include(u => u.Ward)
                .FirstOrDefaultAsync(u => u.Id == currentUserId.Value);

            if (user == null)
                return NotFound("Không tìm thấy người dùng.");

            return Ok(MapUserToDto(user));
        }

        // 🔹 POST: api/users - SystemAdmin tạo user bất kỳ loại nào
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return Conflict("Email đã được sử dụng.");

            // Kiểm tra EnterpriseId nếu là EnterpriseAdmin
            if (dto.Role == "EnterpriseAdmin")
            {
                if (!dto.EnterpriseId.HasValue)
                    return BadRequest("EnterpriseId là bắt buộc khi tạo EnterpriseAdmin.");

                var enterprise = await _context.Enterprises.FindAsync(dto.EnterpriseId.Value);
                if (enterprise == null)
                    return BadRequest("EnterpriseId không hợp lệ.");
            }
            else if (dto.Role != "Customer" && dto.Role != "SystemAdmin")
            {
                return BadRequest("Role không hợp lệ. Phải là SystemAdmin, EnterpriseAdmin hoặc Customer.");
            }

            var user = new User
            {
                Name = dto.Name.Trim(),
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                EnterpriseId = dto.Role == "EnterpriseAdmin" ? dto.EnterpriseId : null,
                PhoneNumber = dto.PhoneNumber?.Trim(),
                Gender = dto.Gender?.Trim(),
                DateOfBirth = dto.DateOfBirth,
                IsActive = dto.IsActive,
                IsEmailVerified = dto.IsEmailVerified
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (dto.Role == "EnterpriseAdmin" && dto.EnterpriseId.HasValue)
            {
                user.Enterprise = await _context.Enterprises.FindAsync(dto.EnterpriseId.Value);
            }

            var userDto = MapUserToDto(user);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
        }

        // 🔹 PUT: api/users/{id} - SystemAdmin cập nhật user
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users
                .Include(u => u.Enterprise)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound("Không tìm thấy user.");

            var hasChanges = false;

            // Cập nhật Name
            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name.Trim() != user.Name)
            {
                user.Name = dto.Name.Trim();
                hasChanges = true;
            }

            // Cập nhật Email
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var newEmail = dto.Email.Trim().ToLower();
                if (!newEmail.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _context.Users.AnyAsync(u => u.Email == newEmail && u.Id != id))
                        return Conflict("Email đã được sử dụng bởi người dùng khác.");

                    user.Email = newEmail;
                    hasChanges = true;
                }
            }

            // Cập nhật Role
            if (!string.IsNullOrWhiteSpace(dto.Role) && dto.Role != user.Role)
            {
                if (dto.Role != "SystemAdmin" && dto.Role != "EnterpriseAdmin" && dto.Role != "Customer")
                    return BadRequest("Role không hợp lệ.");

                user.Role = dto.Role;
                hasChanges = true;

                // Nếu đổi role khỏi EnterpriseAdmin, xóa EnterpriseId
                if (dto.Role != "EnterpriseAdmin")
                {
                    user.EnterpriseId = null;
                }
            }

            // Cập nhật EnterpriseId (chỉ khi là EnterpriseAdmin)
            if (dto.EnterpriseId.HasValue)
            {
                if (user.Role != "EnterpriseAdmin")
                    return BadRequest("Chỉ EnterpriseAdmin mới có thể có EnterpriseId.");

                var enterprise = await _context.Enterprises.FindAsync(dto.EnterpriseId.Value);
                if (enterprise == null)
                    return BadRequest("EnterpriseId không hợp lệ.");

                if (user.EnterpriseId != dto.EnterpriseId.Value)
                {
                    user.EnterpriseId = dto.EnterpriseId.Value;
                    user.Enterprise = enterprise;
                    hasChanges = true;
                }
            }
            else if (user.Role == "EnterpriseAdmin" && dto.EnterpriseId.HasValue == false && user.EnterpriseId.HasValue)
            {
                // Xóa EnterpriseId nếu được set về null
                user.EnterpriseId = null;
                user.Enterprise = null;
                hasChanges = true;
            }

            // Cập nhật các trường khác
            if (dto.PhoneNumber != null && dto.PhoneNumber.Trim() != user.PhoneNumber)
            {
                user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
                hasChanges = true;
            }

            if (dto.Gender != null && dto.Gender.Trim() != user.Gender)
            {
                user.Gender = string.IsNullOrWhiteSpace(dto.Gender) ? null : dto.Gender.Trim();
                hasChanges = true;
            }

            if (dto.DateOfBirth.HasValue && dto.DateOfBirth != user.DateOfBirth)
            {
                user.DateOfBirth = dto.DateOfBirth;
                hasChanges = true;
            }

            if (dto.ShippingAddress != null && dto.ShippingAddress.Trim() != user.ShippingAddress)
            {
                user.ShippingAddress = string.IsNullOrWhiteSpace(dto.ShippingAddress) ? null : dto.ShippingAddress.Trim();
                hasChanges = true;
            }

            if (dto.AvatarUrl != null && dto.AvatarUrl.Trim() != user.AvatarUrl)
            {
                user.AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl) ? null : dto.AvatarUrl.Trim();
                hasChanges = true;
            }

            if (dto.IsActive.HasValue && dto.IsActive.Value != user.IsActive)
            {
                user.IsActive = dto.IsActive.Value;
                hasChanges = true;
            }

            if (dto.IsEmailVerified.HasValue && dto.IsEmailVerified.Value != user.IsEmailVerified)
            {
                user.IsEmailVerified = dto.IsEmailVerified.Value;
                hasChanges = true;
            }

            // Cập nhật địa chỉ chi tiết
            if (dto.ProvinceId.HasValue || dto.DistrictId.HasValue || dto.WardId.HasValue || !string.IsNullOrWhiteSpace(dto.AddressDetail))
            {
                // Validate địa chỉ nếu có thay đổi
                if (dto.ProvinceId.HasValue)
                {
                    var province = await _context.Provinces.FindAsync(dto.ProvinceId.Value);
                    if (province == null)
                        return BadRequest($"Không tìm thấy tỉnh/thành phố với Id = {dto.ProvinceId.Value}.");

                    user.ProvinceId = dto.ProvinceId.Value;
                    hasChanges = true;
                }

                if (dto.DistrictId.HasValue)
                {
                    var district = await _context.Districts
                        .FirstOrDefaultAsync(d => d.Id == dto.DistrictId.Value && 
                                                 (!dto.ProvinceId.HasValue || d.ProvinceId == dto.ProvinceId.Value));
                    if (district == null)
                        return BadRequest($"Không tìm thấy quận/huyện với Id = {dto.DistrictId.Value}.");

                    user.DistrictId = dto.DistrictId.Value;
                    hasChanges = true;
                }

                if (dto.WardId.HasValue)
                {
                    var ward = await _context.Wards
                        .FirstOrDefaultAsync(w => w.Id == dto.WardId.Value &&
                                                (!dto.DistrictId.HasValue || w.DistrictId == dto.DistrictId.Value));
                    if (ward == null)
                        return BadRequest($"Không tìm thấy phường/xã với Id = {dto.WardId.Value}.");

                    user.WardId = dto.WardId.Value;
                    hasChanges = true;
                }

                if (!string.IsNullOrWhiteSpace(dto.AddressDetail))
                {
                    user.AddressDetail = dto.AddressDetail.Trim();
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Reload để lấy dữ liệu mới nhất
                user = await _context.Users
                    .Include(u => u.Enterprise)
                    .Include(u => u.Province)
                    .Include(u => u.District)
                    .Include(u => u.Ward)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound("Không tìm thấy user sau khi cập nhật.");
            }

            return Ok(MapUserToDto(user));
        }

        // 🔹 PUT: api/users/{id}/toggle-status - SystemAdmin vô hiệu hóa/kích hoạt tài khoản
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut("{id}/toggle-status")]
        public async Task<ActionResult<UserDto>> ToggleUserStatus(int id, [FromBody] ToggleUserStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users
                .Include(u => u.Enterprise)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound("Không tìm thấy user.");

            // Không cho phép vô hiệu hóa chính mình
            var currentUserId = await GetCurrentUserIdAsync();
            if (currentUserId.HasValue && user.Id == currentUserId.Value && !dto.IsActive)
                return BadRequest("Bạn không thể vô hiệu hóa tài khoản của chính mình.");

            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(MapUserToDto(user));
        }

        // 🔹 DELETE: api/users/{id} - Chỉ SystemAdmin
        [Authorize(Roles = "SystemAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
