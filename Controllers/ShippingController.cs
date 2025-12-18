using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IShippingService _shippingService;

        public ShippingController(AppDbContext context, IShippingService shippingService)
        {
            _context = context;
            _shippingService = shippingService;
        }

        /// <summary>
        /// Tính phí ship dựa trên tỉnh/thành phố người mua
        /// </summary>
        [HttpGet("calculate")]
        public async Task<ActionResult<object>> CalculateShippingFee([FromQuery] string province)
        {
            if (string.IsNullOrWhiteSpace(province))
                return BadRequest("Vui lòng cung cấp tỉnh/thành phố.");

            var (fee, zoneType, zoneName) = await _shippingService.CalculateShippingFeeAsync(province);

            return Ok(new
            {
                province,
                zoneType,
                zoneName,
                shippingFee = fee
            });
        }

        /// <summary>
        /// Lấy danh sách các quy tắc phí ship
        /// </summary>
        [HttpGet("rules")]
        public async Task<ActionResult<List<ShippingRule>>> GetShippingRules()
        {
            var rules = await _shippingService.GetAllShippingRulesAsync();
            return Ok(rules);
        }

        /// <summary>
        /// Tạo/cập nhật shipping rule (SystemAdmin only)
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost("rules")]
        public async Task<ActionResult<ShippingRule>> CreateOrUpdateRule([FromBody] CreateShippingRuleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var validZoneTypes = new[] { "SameProvince", "SameRegion", "DifferentRegion" };
            if (!validZoneTypes.Contains(dto.ZoneType))
                return BadRequest($"ZoneType không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validZoneTypes)}");

            var existingRule = await _context.ShippingRules
                .FirstOrDefaultAsync(r => r.ZoneType == dto.ZoneType);

            if (existingRule != null)
            {
                // Update
                existingRule.DisplayName = dto.DisplayName;
                existingRule.ShippingFee = dto.ShippingFee;
                existingRule.Description = dto.Description;
                existingRule.IsActive = dto.IsActive;
                existingRule.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create
                existingRule = new ShippingRule
                {
                    ZoneType = dto.ZoneType,
                    DisplayName = dto.DisplayName,
                    ShippingFee = dto.ShippingFee,
                    Description = dto.Description,
                    IsActive = dto.IsActive
                };
                _context.ShippingRules.Add(existingRule);
            }

            await _context.SaveChangesAsync();
            return Ok(existingRule);
        }

        /// <summary>
        /// Xóa shipping rule (SystemAdmin only)
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpDelete("rules/{id}")]
        public async Task<IActionResult> DeleteRule(int id)
        {
            var rule = await _context.ShippingRules.FindAsync(id);
            if (rule == null)
                return NotFound("Không tìm thấy quy tắc phí ship.");

            _context.ShippingRules.Remove(rule);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Seed dữ liệu mặc định cho shipping rules (SystemAdmin only)
        /// </summary>
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost("rules/seed")]
        public async Task<ActionResult<List<ShippingRule>>> SeedDefaultRules()
        {
            var existingRules = await _context.ShippingRules.AnyAsync();
            if (existingRules)
                return BadRequest("Đã có dữ liệu shipping rules. Vui lòng xóa trước khi seed.");

            var defaultRules = new List<ShippingRule>
            {
                new ShippingRule
                {
                    ZoneType = "SameProvince",
                    DisplayName = "Cùng tỉnh",
                    ShippingFee = 20000,
                    Description = "Giao hàng trong cùng tỉnh Gia Lai",
                    IsActive = true
                },
                new ShippingRule
                {
                    ZoneType = "SameRegion",
                    DisplayName = "Cùng miền",
                    ShippingFee = 30000,
                    Description = "Giao hàng trong miền Trung và Tây Nguyên",
                    IsActive = true
                },
                new ShippingRule
                {
                    ZoneType = "DifferentRegion",
                    DisplayName = "Khác miền",
                    ShippingFee = 40000,
                    Description = "Giao hàng đến miền Bắc hoặc miền Nam",
                    IsActive = true
                }
            };

            _context.ShippingRules.AddRange(defaultRules);
            await _context.SaveChangesAsync();

            return Ok(defaultRules);
        }
    }

    public class CreateShippingRuleDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string ZoneType { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public string DisplayName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(0, double.MaxValue)]
        public decimal ShippingFee { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

