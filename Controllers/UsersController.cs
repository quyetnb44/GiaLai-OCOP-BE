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
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UsersController(AppDbContext context) => _context = context;

        // 🔹 GET: api/users
        // Chỉ SystemAdmin xem tất cả user
        [Authorize(Roles = "SystemAdmin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Enterprise)
                .ToListAsync();

            var usersDto = users.Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                EnterpriseId = u.EnterpriseId,
                Enterprise = u.Enterprise == null ? null : new EnterpriseDto
                {
                    Id = u.Enterprise.Id,
                    Name = u.Enterprise.Name,
                    Description = u.Enterprise.Description
                }
            }).ToList();

            return Ok(usersDto);
        }

        // 🔹 GET: api/users/{id}
        // SystemAdmin xem tất cả, Customer/EnterpriseAdmin xem chính mình
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var targetUser = await _context.Users.Include(u => u.Enterprise)
                                           .FirstOrDefaultAsync(u => u.Id == id);
            if (targetUser == null) return NotFound();

            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            int? currentUserId = null;
            if (!string.IsNullOrWhiteSpace(claimValue))
            {
                if (int.TryParse(claimValue, out var userId))
                    currentUserId = userId;
                else if (claimValue.Contains("@"))
                {
                    var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == claimValue);
                    currentUserId = currentUser?.Id;
                }
            }

            if (currentUserId == null) return Forbid();

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "SystemAdmin" && currentUserId.Value != id)
                return Forbid();

            var userDto = new UserDto
            {
                Id = targetUser.Id,
                Name = targetUser.Name,
                Email = targetUser.Email,
                Role = targetUser.Role,
                EnterpriseId = targetUser.EnterpriseId,
                Enterprise = targetUser.Enterprise == null ? null : new EnterpriseDto
                {
                    Id = targetUser.Enterprise.Id,
                    Name = targetUser.Enterprise.Name,
                    Description = targetUser.Enterprise.Description
                }
            };

            return Ok(userDto);
        }

        // 🔹 POST: api/users/enterprise-admin
        // Chỉ SystemAdmin tạo EnterpriseAdmin
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost("enterprise-admin")]
        public async Task<ActionResult<UserDto>> CreateEnterpriseAdmin([FromBody] CreateEnterpriseAdminDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict("Email đã được sử dụng.");

            var enterprise = await _context.Enterprises.FindAsync(dto.EnterpriseId);
            if (enterprise == null)
                return BadRequest("EnterpriseId không hợp lệ.");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "EnterpriseAdmin",
                EnterpriseId = dto.EnterpriseId
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                EnterpriseId = user.EnterpriseId,
                Enterprise = new EnterpriseDto
                {
                    Id = enterprise.Id,
                    Name = enterprise.Name,
                    Description = enterprise.Description
                }
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
        }

        // 🔹 POST: api/users/customer
        // Mọi người tự đăng ký Customer
        [AllowAnonymous]
        [HttpPost("customer")]
        public async Task<ActionResult<UserDto>> CreateCustomer([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict("Email đã được sử dụng.");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Customer"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
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
