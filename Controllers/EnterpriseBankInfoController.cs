using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace GiaLaiOCOP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnterpriseBankInfoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IVietQrService _vietQrService;
        private readonly ILogger<EnterpriseBankInfoController> _logger;

        public EnterpriseBankInfoController(
            AppDbContext context, 
            IVietQrService vietQrService,
            ILogger<EnterpriseBankInfoController> logger)
        {
            _context = context;
            _vietQrService = vietQrService;
            _logger = logger;
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

        // 🔹 GET: api/enterprise-bank-info/me - EnterpriseAdmin xem thông tin ngân hàng của mình
        [HttpGet("me")]
        [Authorize(Roles = "EnterpriseAdmin")]
        public async Task<ActionResult<EnterpriseBankInfoDto>> GetMyEnterpriseBankInfo()
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var user = await _context.Users
                .Include(u => u.Enterprise)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null || user.EnterpriseId == null)
                return NotFound("Bạn không thuộc doanh nghiệp nào.");

            var bankInfo = await _context.EnterpriseBankInfos
                .FirstOrDefaultAsync(ebi => ebi.EnterpriseId == user.EnterpriseId.Value);

            if (bankInfo == null)
                return NotFound("Chưa cấu hình thông tin ngân hàng.");

            return Ok(MapToDto(bankInfo));
        }

        // 🔹 POST: api/enterprise-bank-info - EnterpriseAdmin tạo/cập nhật thông tin ngân hàng
        [HttpPost]
        [Authorize(Roles = "EnterpriseAdmin")]
        public async Task<ActionResult<EnterpriseBankInfoDto>> CreateOrUpdateEnterpriseBankInfo([FromBody] CreateEnterpriseBankInfoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var user = await _context.Users
                .Include(u => u.Enterprise)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null || user.EnterpriseId == null)
                return NotFound("Bạn không thuộc doanh nghiệp nào.");

            var enterpriseId = user.EnterpriseId.Value;

            // Kiểm tra đã có thông tin ngân hàng chưa
            var existingBankInfo = await _context.EnterpriseBankInfos
                .FirstOrDefaultAsync(ebi => ebi.EnterpriseId == enterpriseId);

            EnterpriseBankInfo bankInfo;

            if (existingBankInfo != null)
            {
                // Cập nhật thông tin hiện có
                existingBankInfo.BankName = dto.BankName;
                existingBankInfo.BankAccount = dto.BankAccount;
                existingBankInfo.AccountName = dto.AccountName;
                existingBankInfo.BankCode = dto.BankCode;
                existingBankInfo.Template = dto.Template;
                existingBankInfo.UpdatedAt = DateTime.UtcNow;

                // Tạo lại QR code với thông tin mới
                try
                {
                    existingBankInfo.QrCodeBase64 = _vietQrService.GenerateAccountQrCodeBase64(
                        dto.BankCode, 
                        dto.BankAccount, 
                        dto.AccountName
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating QR code for Enterprise {EnterpriseId}", enterpriseId);
                    return StatusCode(500, new { message = "Lỗi khi tạo QR code. Vui lòng thử lại." });
                }

                bankInfo = existingBankInfo;
            }
            else
            {
                // Tạo mới
                try
                {
                    var qrCodeBase64 = _vietQrService.GenerateAccountQrCodeBase64(
                        dto.BankCode, 
                        dto.BankAccount, 
                        dto.AccountName
                    );

                    bankInfo = new EnterpriseBankInfo
                    {
                        EnterpriseId = enterpriseId,
                        BankName = dto.BankName,
                        BankAccount = dto.BankAccount,
                        AccountName = dto.AccountName,
                        BankCode = dto.BankCode,
                        Template = dto.Template,
                        QrCodeBase64 = qrCodeBase64,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.EnterpriseBankInfos.Add(bankInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating QR code for Enterprise {EnterpriseId}", enterpriseId);
                    return StatusCode(500, new { message = "Lỗi khi tạo QR code. Vui lòng thử lại." });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(MapToDto(bankInfo));
        }

        // 🔹 PUT: api/enterprise-bank-info - EnterpriseAdmin cập nhật thông tin ngân hàng
        [HttpPut]
        [Authorize(Roles = "EnterpriseAdmin")]
        public async Task<ActionResult<EnterpriseBankInfoDto>> UpdateEnterpriseBankInfo([FromBody] UpdateEnterpriseBankInfoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var user = await _context.Users
                .Include(u => u.Enterprise)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null || user.EnterpriseId == null)
                return NotFound("Bạn không thuộc doanh nghiệp nào.");

            var bankInfo = await _context.EnterpriseBankInfos
                .FirstOrDefaultAsync(ebi => ebi.EnterpriseId == user.EnterpriseId.Value);

            if (bankInfo == null)
                return NotFound("Chưa cấu hình thông tin ngân hàng. Vui lòng tạo mới.");

            // Cập nhật các trường được cung cấp
            bool needsQrRegeneration = false;

            if (dto.BankName != null)
            {
                bankInfo.BankName = dto.BankName;
            }

            if (dto.BankAccount != null)
            {
                bankInfo.BankAccount = dto.BankAccount;
                needsQrRegeneration = true;
            }

            if (dto.AccountName != null)
            {
                bankInfo.AccountName = dto.AccountName;
                needsQrRegeneration = true;
            }

            if (dto.BankCode != null)
            {
                bankInfo.BankCode = dto.BankCode;
                needsQrRegeneration = true;
            }

            if (dto.Template != null)
            {
                bankInfo.Template = dto.Template;
            }

            // Tạo lại QR code nếu thông tin tài khoản thay đổi
            if (needsQrRegeneration)
            {
                try
                {
                    bankInfo.QrCodeBase64 = _vietQrService.GenerateAccountQrCodeBase64(
                        bankInfo.BankCode, 
                        bankInfo.BankAccount, 
                        bankInfo.AccountName
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error regenerating QR code for Enterprise {EnterpriseId}", bankInfo.EnterpriseId);
                    return StatusCode(500, new { message = "Lỗi khi tạo lại QR code. Vui lòng thử lại." });
                }
            }

            bankInfo.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(MapToDto(bankInfo));
        }

        // 🔹 GET: api/enterprise-bank-info/enterprise/{enterpriseId} - Public endpoint để lấy QR code khi thanh toán
        [HttpGet("enterprise/{enterpriseId}")]
        [AllowAnonymous]
        public async Task<ActionResult<EnterpriseBankInfoDto>> GetEnterpriseBankInfo(int enterpriseId)
        {
            var bankInfo = await _context.EnterpriseBankInfos
                .FirstOrDefaultAsync(ebi => ebi.EnterpriseId == enterpriseId);

            if (bankInfo == null)
                return NotFound("Enterprise chưa cấu hình thông tin ngân hàng.");

            return Ok(MapToDto(bankInfo));
        }

        // 🔹 Helper: Map to DTO
        private static EnterpriseBankInfoDto MapToDto(EnterpriseBankInfo bankInfo)
        {
            return new EnterpriseBankInfoDto
            {
                Id = bankInfo.Id,
                EnterpriseId = bankInfo.EnterpriseId,
                BankName = bankInfo.BankName,
                BankAccount = bankInfo.BankAccount,
                AccountName = bankInfo.AccountName,
                BankCode = bankInfo.BankCode,
                Template = bankInfo.Template,
                QrCodeBase64 = bankInfo.QrCodeBase64,
                CreatedAt = bankInfo.CreatedAt,
                UpdatedAt = bankInfo.UpdatedAt
            };
        }
    }
}

