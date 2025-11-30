using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

        // 🔹 PUT: api/users/{id} - Chỉ SystemAdmin
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id) return BadRequest();
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 🔹 DELETE: api/users/{id} - Chỉ SystemAdmin
        // Xóa User trực tiếp, database sẽ tự động cascade delete các dữ liệu liên quan
        // Chỉ xử lý các trường hợp có Restrict constraint
        [Authorize(Roles = "SystemAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);
                
                if (user == null) 
                {
                    return NotFound(new { message = $"Không tìm thấy người dùng với ID {id}" });
                }

                Console.WriteLine($"🔹 Bắt đầu xóa user {id}: {user.Name} ({user.Email})");

                // 🔹 Chỉ xử lý các trường hợp có Restrict constraint (không thể cascade delete)
                // 1. Orders có Restrict - cần xóa trước
                var orders = await _context.Orders
                    .Where(o => o.UserId == id)
                    .ToListAsync();
                if (orders.Any())
                {
                    Console.WriteLine($"🔹 Xóa {orders.Count} Orders (Restrict constraint)...");
                    // Xóa OrderItems và Payments sẽ được cascade delete tự động
                    _context.Orders.RemoveRange(orders);
                }

                // 2. Images được upload bởi user (UploadedByUserId) có Restrict - cần set null
                var imagesUploadedByUser = await _context.Images
                    .Where(img => img.UploadedByUserId == id)
                    .ToListAsync();
                if (imagesUploadedByUser.Any())
                {
                    Console.WriteLine($"🔹 Set null cho {imagesUploadedByUser.Count} Images (UploadedByUserId - Restrict constraint)...");
                    foreach (var img in imagesUploadedByUser)
                    {
                        img.UploadedByUserId = null;
                    }
                }

                // 3. InventoryHistory có SetNull - sẽ tự động set null, nhưng xử lý để chắc chắn
                var inventoryHistories = await _context.InventoryHistories
                    .Where(ih => ih.CreatedByUserId == id)
                    .ToListAsync();
                if (inventoryHistories.Any())
                {
                    Console.WriteLine($"🔹 Set null cho {inventoryHistories.Count} InventoryHistories (SetNull constraint)...");
                    foreach (var ih in inventoryHistories)
                    {
                        ih.CreatedByUserId = null;
                    }
                }

                // 4. Products có ApprovedByUserId - cần set null
                var productsApprovedByUser = await _context.Products
                    .Where(p => p.ApprovedByUserId == id)
                    .ToListAsync();
                if (productsApprovedByUser.Any())
                {
                    Console.WriteLine($"🔹 Set null cho {productsApprovedByUser.Count} Products (ApprovedByUserId)...");
                    foreach (var product in productsApprovedByUser)
                    {
                        product.ApprovedByUserId = null;
                    }
                }

                // 🔹 Xóa User trực tiếp - Database sẽ tự động cascade delete:
                // - ShippingAddresses (Cascade)
                // - Notifications (Cascade)
                // - Images (avatar - Cascade)
                // - Reviews (sẽ được xóa)
                // - EnterpriseApplications (sẽ được xóa)
                Console.WriteLine($"🔹 Xóa User {id} trực tiếp (database sẽ tự động xóa các dữ liệu liên quan)...");
                _context.Users.Remove(user);
                
                // Lưu tất cả thay đổi
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Đã xóa user {id} thành công! Database đã tự động xóa các dữ liệu liên quan.");
                
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                // Log lỗi chi tiết
                var errorMessage = $"Error deleting user {id}: {ex.Message}";
                Console.WriteLine($"❌ {errorMessage}");
                
                if (ex.InnerException != null)
                {
                    var innerMessage = $"Inner exception: {ex.InnerException.Message}";
                    Console.WriteLine($"❌ {innerMessage}");
                    errorMessage += $" | {innerMessage}";
                }

                // Trả về lỗi với thông tin chi tiết
                return StatusCode(500, new { 
                    message = "Lỗi khi xóa người dùng. Vui lòng thử lại sau.", 
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                // Bắt tất cả các lỗi khác
                var errorMessage = $"Unexpected error deleting user {id}: {ex.Message}";
                Console.WriteLine($"❌ {errorMessage}");
                
                return StatusCode(500, new { 
                    message = "Lỗi không mong đợi khi xóa người dùng.", 
                    error = ex.Message
                });
            }
        }
    }
}
