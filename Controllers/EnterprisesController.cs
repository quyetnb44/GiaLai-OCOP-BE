using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

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
                ApprovalStatus = enterprise.ApprovalStatus,
                RejectionReason = enterprise.RejectionReason,
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
                StockQuantity = p.StockQuantity,
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

        private EnterpriseSettingsDto MapSettingsToDto(EnterpriseSettings settings)
        {
            var shippingMethods = ParseShippingMethods(settings.ShippingMethodsJson);

            return new EnterpriseSettingsDto
            {
                EnterpriseId = settings.EnterpriseId,
                ShippingMethods = shippingMethods,
                ContactEmail = settings.ContactEmail,
                ContactPhone = settings.ContactPhone,
                ContactAddress = settings.ContactAddress,
                BusinessHours = settings.BusinessHours,
                ReturnPolicy = settings.ReturnPolicy,
                ShippingPolicy = settings.ShippingPolicy,
                UpdatedAt = settings.UpdatedAt ?? settings.CreatedAt
            };
        }

        private List<ShippingMethodDto> ParseShippingMethods(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return GetDefaultShippingMethods();

            try
            {
                var methods = JsonSerializer.Deserialize<List<ShippingMethodDto>>(json);
                return methods ?? GetDefaultShippingMethods();
            }
            catch
            {
                return GetDefaultShippingMethods();
            }
        }

        private List<ShippingMethodDto> GetDefaultShippingMethods()
        {
            return new List<ShippingMethodDto>
            {
                new ShippingMethodDto { Id = "cod", Name = "COD", Enabled = true, Fee = 0 },
                new ShippingMethodDto { Id = "standard", Name = "Giao hàng tiêu chuẩn", Enabled = true, Fee = 30000 },
                new ShippingMethodDto { Id = "express", Name = "Giao hàng nhanh", Enabled = false, Fee = 50000 }
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

        /// <summary>
        /// Lấy cài đặt doanh nghiệp hiện tại
        /// </summary>
        [HttpGet("me/settings")]
        [Authorize(Roles = "EnterpriseAdmin")]
        public async Task<ActionResult<EnterpriseSettingsDto>> GetMyEnterpriseSettings()
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var user = await _context.Users
                .Include(u => u.Enterprise)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user?.EnterpriseId == null || user.Enterprise == null)
                return NotFound("Bạn không thuộc doanh nghiệp nào.");

            var settings = await _context.EnterpriseSettings
                .FirstOrDefaultAsync(s => s.EnterpriseId == user.EnterpriseId.Value);

            if (settings == null)
            {
                return Ok(new EnterpriseSettingsDto
                {
                    EnterpriseId = user.EnterpriseId.Value,
                    ShippingMethods = GetDefaultShippingMethods(),
                    ContactEmail = user.Enterprise.EmailContact,
                    ContactPhone = user.Enterprise.PhoneNumber,
                    ContactAddress = user.Enterprise.Address,
                    BusinessHours = "08:00 - 17:00",
                    ReturnPolicy = string.Empty,
                    ShippingPolicy = string.Empty,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            return Ok(MapSettingsToDto(settings));
        }

        /// <summary>
        /// Cập nhật cài đặt doanh nghiệp
        /// </summary>
        [HttpPut("me/settings")]
        [Authorize(Roles = "EnterpriseAdmin")]
        public async Task<ActionResult<EnterpriseSettingsDto>> UpdateMyEnterpriseSettings([FromBody] EnterpriseSettingsDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var user = await _context.Users
                .Include(u => u.Enterprise)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user?.EnterpriseId == null || user.Enterprise == null)
                return NotFound("Bạn không thuộc doanh nghiệp nào.");

            var settings = await _context.EnterpriseSettings
                .FirstOrDefaultAsync(s => s.EnterpriseId == user.EnterpriseId.Value);

            if (settings == null)
            {
                settings = new EnterpriseSettings
                {
                    EnterpriseId = user.EnterpriseId.Value,
                    CreatedAt = DateTime.UtcNow
                };
                _context.EnterpriseSettings.Add(settings);
            }

            settings.ContactEmail = string.IsNullOrWhiteSpace(dto.ContactEmail)
                ? user.Enterprise.EmailContact
                : dto.ContactEmail;
            settings.ContactPhone = string.IsNullOrWhiteSpace(dto.ContactPhone)
                ? user.Enterprise.PhoneNumber
                : dto.ContactPhone;
            settings.ContactAddress = string.IsNullOrWhiteSpace(dto.ContactAddress)
                ? user.Enterprise.Address
                : dto.ContactAddress;
            settings.BusinessHours = string.IsNullOrWhiteSpace(dto.BusinessHours) ? "08:00 - 17:00" : dto.BusinessHours;
            settings.ReturnPolicy = dto.ReturnPolicy;
            settings.ShippingPolicy = dto.ShippingPolicy;
            settings.ShippingMethodsJson = JsonSerializer.Serialize(dto.ShippingMethods?.Any() == true
                ? dto.ShippingMethods
                : GetDefaultShippingMethods());
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(MapSettingsToDto(settings));
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
            enterprise.Name = dto.Name ?? string.Empty;
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
        // Cập nhật thông tin enterprise (chấp nhận UpdateEnterpriseDto - chỉ cần gửi các trường muốn cập nhật)
        [HttpPut("{id}")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<ActionResult<EnterpriseDto>> UpdateEnterprise(int id, [FromBody] UpdateEnterpriseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var enterprise = await _context.Enterprises
                .Include(e => e.Products)!
                    .ThenInclude(p => p.Category)
                .Include(e => e.Users)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enterprise == null)
                return NotFound();

            var hasChanges = false;

            // Cập nhật các trường được cung cấp
            if (dto.Name != null && !string.IsNullOrWhiteSpace(dto.Name) && dto.Name.Trim() != enterprise.Name)
            {
                enterprise.Name = dto.Name.Trim();
                hasChanges = true;
            }

            if (dto.Description != null)
            {
                var normalizedDescription = string.IsNullOrWhiteSpace(dto.Description) ? string.Empty : dto.Description.Trim();
                if (normalizedDescription != enterprise.Description)
                {
                    enterprise.Description = normalizedDescription;
                    hasChanges = true;
                }
            }

            if (dto.Address != null)
            {
                var normalizedAddress = string.IsNullOrWhiteSpace(dto.Address) ? string.Empty : dto.Address.Trim();
                if (normalizedAddress != enterprise.Address)
                {
                    enterprise.Address = normalizedAddress;
                    hasChanges = true;
                }
            }

            if (dto.Ward != null)
            {
                var normalizedWard = string.IsNullOrWhiteSpace(dto.Ward) ? string.Empty : dto.Ward.Trim();
                if (normalizedWard != enterprise.Ward)
                {
                    enterprise.Ward = normalizedWard;
                    hasChanges = true;
                }
            }

            if (dto.District != null)
            {
                var normalizedDistrict = string.IsNullOrWhiteSpace(dto.District) ? string.Empty : dto.District.Trim();
                if (normalizedDistrict != enterprise.District)
                {
                    enterprise.District = normalizedDistrict;
                    hasChanges = true;
                }
            }

            if (dto.Province != null)
            {
                var normalizedProvince = string.IsNullOrWhiteSpace(dto.Province) ? string.Empty : dto.Province.Trim();
                if (normalizedProvince != enterprise.Province)
                {
                    enterprise.Province = normalizedProvince;
                    hasChanges = true;
                }
            }

            if (dto.Latitude.HasValue && dto.Latitude.Value != enterprise.Latitude)
            {
                enterprise.Latitude = dto.Latitude.Value;
                hasChanges = true;
            }

            if (dto.Longitude.HasValue && dto.Longitude.Value != enterprise.Longitude)
            {
                enterprise.Longitude = dto.Longitude.Value;
                hasChanges = true;
            }

            if (dto.PhoneNumber != null)
            {
                var normalizedPhone = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? string.Empty : dto.PhoneNumber.Trim();
                if (normalizedPhone != enterprise.PhoneNumber)
                {
                    enterprise.PhoneNumber = normalizedPhone;
                    hasChanges = true;
                }
            }

            if (dto.EmailContact != null)
            {
                var normalizedEmail = string.IsNullOrWhiteSpace(dto.EmailContact) ? string.Empty : dto.EmailContact.Trim().ToLower();
                if (normalizedEmail != enterprise.EmailContact)
                {
                    enterprise.EmailContact = normalizedEmail;
                    hasChanges = true;
                }
            }

            if (dto.Website != null)
            {
                var normalizedWebsite = string.IsNullOrWhiteSpace(dto.Website) ? string.Empty : dto.Website.Trim();
                if (normalizedWebsite != enterprise.Website)
                {
                    enterprise.Website = normalizedWebsite;
                    hasChanges = true;
                }
            }

            if (dto.BusinessField != null)
            {
                var normalizedField = string.IsNullOrWhiteSpace(dto.BusinessField) ? string.Empty : dto.BusinessField.Trim();
                if (normalizedField != enterprise.BusinessField)
                {
                    enterprise.BusinessField = normalizedField;
                    hasChanges = true;
                }
            }

            if (dto.ImageUrl != null)
            {
                var normalizedImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
                if (normalizedImageUrl != enterprise.ImageUrl)
                {
                    enterprise.ImageUrl = normalizedImageUrl;
                    hasChanges = true;
                }
            }

            if (dto.OCOPRating.HasValue && dto.OCOPRating.Value != enterprise.OCOPRating)
            {
                enterprise.OCOPRating = dto.OCOPRating.Value;
                hasChanges = true;
            }

            if (dto.ApprovalStatus != null)
            {
                var normalizedStatus = string.IsNullOrWhiteSpace(dto.ApprovalStatus) ? string.Empty : dto.ApprovalStatus.Trim();
                if (normalizedStatus != enterprise.ApprovalStatus)
                {
                    enterprise.ApprovalStatus = normalizedStatus;
                    hasChanges = true;
                }
            }

            if (dto.RejectionReason != null)
            {
                var normalizedReason = string.IsNullOrWhiteSpace(dto.RejectionReason) ? null : dto.RejectionReason.Trim();
                if (normalizedReason != enterprise.RejectionReason)
                {
                    enterprise.RejectionReason = normalizedReason;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                enterprise.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Reload để lấy dữ liệu mới nhất
            enterprise = await _context.Enterprises
                .Include(e => e.Products)!
                    .ThenInclude(p => p.Category)
                .Include(e => e.Users)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enterprise == null)
                return NotFound();

            return Ok(MapEnterpriseToDto(enterprise));
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
