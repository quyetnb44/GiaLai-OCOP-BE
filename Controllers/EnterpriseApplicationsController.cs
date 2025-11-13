using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace GiaLaiOCOP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnterpriseApplicationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public EnterpriseApplicationsController(AppDbContext context)
        {
            _context = context;
        }

        // 🟢 CUSTOMER gửi đơn đăng ký OCOP
        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> Apply([FromBody] CreateEnterpriseApplicationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 🔹 Lấy thông tin userId từ token
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(claimValue))
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            User? applicant = null;
            int userId;
            if (claimValue.Contains("@"))
            {
                applicant = await _context.Users.FirstOrDefaultAsync(u => u.Email == claimValue);
                if (applicant == null) return Unauthorized("Người dùng không tồn tại.");
                userId = applicant.Id;
            }
            else if (!int.TryParse(claimValue, out userId))
            {
                return Unauthorized("Token không hợp lệ hoặc sai định dạng.");
            }
            else
            {
                applicant = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (applicant == null)
                    return Unauthorized("Người dùng không tồn tại.");
            }

            if (applicant!.Role == "EnterpriseAdmin")
                return BadRequest("Tài khoản của bạn đã là EnterpriseAdmin.");

            // 🔹 Kiểm tra nếu có đơn pending
            var hasPending = await _context.EnterpriseApplications
                .AnyAsync(a => a.UserId == userId && a.Status == "Pending");

            if (hasPending)
                return BadRequest("Bạn đã có một đơn đăng ký đang chờ duyệt.");

            // 🔹 Tạo đơn mới
            var app = new EnterpriseApplication
            {
                UserId = userId,
                EnterpriseName = dto.EnterpriseName,
                BusinessType = dto.BusinessType,
                TaxCode = dto.TaxCode,
                BusinessLicenseNumber = dto.BusinessLicenseNumber,
                LicenseIssuedDate = dto.LicenseIssuedDate,
                LicenseIssuedBy = dto.LicenseIssuedBy,
                Address = dto.Address,
                Ward = dto.Ward,
                District = dto.District,
                Province = dto.Province,
                PhoneNumber = dto.PhoneNumber,
                EmailContact = dto.EmailContact,
                Website = dto.Website,
                RepresentativeName = dto.RepresentativeName,
                RepresentativePosition = dto.RepresentativePosition,
                RepresentativeIdNumber = dto.RepresentativeIdNumber,
                RepresentativeIdIssuedDate = dto.RepresentativeIdIssuedDate,
                RepresentativeIdIssuedBy = dto.RepresentativeIdIssuedBy,
                ProductionLocation = dto.ProductionLocation,
                NumberOfEmployees = dto.NumberOfEmployees,
                ProductionScale = dto.ProductionScale,
                BusinessField = dto.BusinessField,
                ProductName = dto.ProductName,
                ProductCategory = dto.ProductCategory,
                ProductDescription = dto.ProductDescription,
                ProductOrigin = dto.ProductOrigin,
                ProductCertifications = dto.ProductCertifications,
                ProductImages = dto.ProductImages,
                AttachedDocuments = dto.AttachedDocuments,
                AdditionalNotes = dto.AdditionalNotes,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.EnterpriseApplications.Add(app);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đơn đăng ký OCOP đã được gửi thành công.",
                app.Id
            });
        }

        // 🟣 ADMIN xem tất cả đơn
        [Authorize(Roles = "SystemAdmin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var apps = await _context.EnterpriseApplications
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Ok(apps.Select(a => new EnterpriseApplicationDto
            {
                Id = a.Id,
                UserId = a.UserId,
                EnterpriseName = a.EnterpriseName,
                BusinessType = a.BusinessType,
                TaxCode = a.TaxCode,
                BusinessLicenseNumber = a.BusinessLicenseNumber,
                LicenseIssuedDate = a.LicenseIssuedDate,
                LicenseIssuedBy = a.LicenseIssuedBy,
                Address = a.Address,
                Ward = a.Ward,
                District = a.District,
                Province = a.Province,
                PhoneNumber = a.PhoneNumber,
                EmailContact = a.EmailContact,
                Website = a.Website,
                RepresentativeName = a.RepresentativeName,
                RepresentativePosition = a.RepresentativePosition,
                RepresentativeIdNumber = a.RepresentativeIdNumber,
                ProductionLocation = a.ProductionLocation,
                NumberOfEmployees = a.NumberOfEmployees,
                ProductionScale = a.ProductionScale,
                BusinessField = a.BusinessField,
                ProductName = a.ProductName,
                ProductCategory = a.ProductCategory,
                ProductDescription = a.ProductDescription,
                ProductOrigin = a.ProductOrigin,
                ProductCertifications = a.ProductCertifications,
                ProductImages = a.ProductImages,
                AttachedDocuments = a.AttachedDocuments,
                AdditionalNotes = a.AdditionalNotes,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            }));
        }

        // 🟡 ADMIN phê duyệt đơn
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var app = await _context.EnterpriseApplications.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
            if (app == null) return NotFound("Không tìm thấy đơn đăng ký.");
            if (app.Status != "Pending") return BadRequest("Đơn đã được xử lý.");

            if (app.User == null)
                return BadRequest("Không tìm thấy thông tin người dùng nộp đơn.");

            if (app.User.Role == "EnterpriseAdmin" && app.User.EnterpriseId.HasValue)
                return BadRequest("Người dùng đã thuộc một doanh nghiệp khác.");

            var enterprise = new Enterprise
            {
                Name = app.EnterpriseName,
                Description = string.IsNullOrWhiteSpace(app.ProductDescription) ? app.BusinessField : app.ProductDescription,
                Address = app.Address,
                Ward = app.Ward,
                District = app.District,
                Province = app.Province,
                PhoneNumber = app.PhoneNumber,
                EmailContact = app.EmailContact,
                Website = app.Website,
                BusinessField = app.BusinessField,
                ImageUrl = GetPrimaryImage(app.ProductImages)
            };

            _context.Enterprises.Add(enterprise);
            await _context.SaveChangesAsync();

            if (app.User != null)
            {
                app.User.Role = "EnterpriseAdmin";
                app.User.EnterpriseId = enterprise.Id;
            }

            app.Status = "Approved";
            app.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã phê duyệt và tạo hồ sơ doanh nghiệp OCOP thành công." });
        }

        // 🔴 ADMIN từ chối đơn
        [Authorize(Roles = "SystemAdmin")]
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] string? comment)
        {
            var app = await _context.EnterpriseApplications.FindAsync(id);
            if (app == null) return NotFound("Không tìm thấy đơn đăng ký.");
            if (app.Status != "Pending") return BadRequest("Đơn đã được xử lý.");

            app.Status = "Rejected";
            app.AdminComment = comment ?? "Không đạt yêu cầu.";
            app.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã từ chối đơn đăng ký OCOP." });
        }

        private static string? GetPrimaryImage(string? imageList)
        {
            if (string.IsNullOrWhiteSpace(imageList))
                return null;

            var parts = imageList
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            return parts.FirstOrDefault();
        }
    }
}
