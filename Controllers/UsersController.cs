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
        // Xóa User và TẤT CẢ dữ liệu liên quan (Orders, OrderItems, Payments, Reviews, v.v.)
        [Authorize(Roles = "SystemAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderItems)
                .Include(u => u.Orders)
                    .ThenInclude(o => o.Payments)
                .Include(u => u.ShippingAddresses)
                .Include(u => u.Images)
                .Include(u => u.Notifications)
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null) return NotFound();

            // 🔹 1. Xóa Orders và tất cả dữ liệu liên quan
            if (user.Orders != null && user.Orders.Any())
            {
                foreach (var order in user.Orders.ToList())
                {
                    // Xóa OrderItems (có cascade nhưng xóa thủ công để chắc chắn)
                    if (order.OrderItems != null && order.OrderItems.Any())
                    {
                        _context.OrderItems.RemoveRange(order.OrderItems);
                    }

                    // Xóa Payments (có cascade nhưng xóa thủ công để chắc chắn)
                    if (order.Payments != null && order.Payments.Any())
                    {
                        _context.Payments.RemoveRange(order.Payments);
                    }

                    // Xóa Notifications liên quan đến Order
                    var orderNotifications = await _context.Notifications
                        .Where(n => n.OrderId == order.Id)
                        .ToListAsync();
                    if (orderNotifications.Any())
                    {
                        _context.Notifications.RemoveRange(orderNotifications);
                    }
                }

                // Xóa tất cả Orders
                _context.Orders.RemoveRange(user.Orders);
            }

            // 🔹 2. Xóa ShippingAddresses
            if (user.ShippingAddresses != null && user.ShippingAddresses.Any())
            {
                _context.ShippingAddresses.RemoveRange(user.ShippingAddresses);
            }

            // 🔹 3. Xóa Notifications
            if (user.Notifications != null && user.Notifications.Any())
            {
                _context.Notifications.RemoveRange(user.Notifications);
            }

            // 🔹 4. Xóa Images liên quan đến user (avatar)
            if (user.Images != null && user.Images.Any())
            {
                _context.Images.RemoveRange(user.Images);
            }

            // 🔹 5. Set null cho Images được upload bởi user này (vì có Restrict)
            var imagesUploadedByUser = await _context.Images
                .Where(img => img.UploadedByUserId == id)
                .ToListAsync();
            foreach (var img in imagesUploadedByUser)
            {
                img.UploadedByUserId = null; // Set null thay vì xóa
            }

            // 🔹 6. Set null cho InventoryHistory được tạo bởi user này
            var inventoryHistories = await _context.InventoryHistories
                .Where(ih => ih.CreatedByUserId == id)
                .ToListAsync();
            foreach (var ih in inventoryHistories)
            {
                ih.CreatedByUserId = null;
            }

            // 🔹 7. Set null cho Product được approve bởi user này
            var productsApprovedByUser = await _context.Products
                .Where(p => p.ApprovedByUserId == id)
                .ToListAsync();
            foreach (var product in productsApprovedByUser)
            {
                product.ApprovedByUserId = null;
            }

            // 🔹 8. Xóa Reviews của user
            var reviews = await _context.Reviews
                .Where(r => r.UserId == id)
                .ToListAsync();
            if (reviews.Any())
            {
                _context.Reviews.RemoveRange(reviews);
            }

            // 🔹 9. Xóa EnterpriseApplications của user
            var enterpriseApplications = await _context.EnterpriseApplications
                .Where(ea => ea.UserId == id)
                .ToListAsync();
            if (enterpriseApplications.Any())
            {
                _context.EnterpriseApplications.RemoveRange(enterpriseApplications);
            }

            // 🔹 10. Cuối cùng mới xóa User
            _context.Users.Remove(user);
            
            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                // Log lỗi chi tiết
                Console.WriteLine($"Error deleting user {id}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = "Lỗi khi xóa người dùng. Vui lòng thử lại sau.", error = ex.Message });
            }
        }
    }
}
