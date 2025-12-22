using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Services.Revenue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SystemAdmin,EnterpriseAdmin")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IRevenueStatisticsService _revenueStatisticsService;
        private readonly GiaLaiOCOP.Api.Services.IRevenueAuthorizationService _authorizationService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            AppDbContext context,
            IRevenueStatisticsService revenueStatisticsService,
            GiaLaiOCOP.Api.Services.IRevenueAuthorizationService authorizationService,
            ILogger<ReportsController> logger)
        {
            _context = context;
            _revenueStatisticsService = revenueStatisticsService;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        /// <summary>
        /// Tổng quan toàn tỉnh: doanh nghiệp, sản phẩm, đơn hàng, thanh toán, hồ sơ OCOP.
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSummary()
        {
            var totalEnterprises = await _context.Enterprises.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var approvedProducts = await _context.Products.CountAsync(p => p.Status == "Approved");
            var pendingProducts = await _context.Products.CountAsync(p => p.Status == "PendingApproval");
            var rejectedProducts = await _context.Products.CountAsync(p => p.Status == "Rejected");

            var totalApplications = await _context.EnterpriseApplications.CountAsync();
            var pendingApplications = await _context.EnterpriseApplications.CountAsync(a => a.Status == "Pending");

            var totalOrders = await _context.Orders.CountAsync();
            var totalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");
            var totalEnterpriseAdmins = await _context.Users.CountAsync(u => u.Role == "EnterpriseAdmin");

            var totalPayments = await _context.Payments.CountAsync();
            var paidPaymentsAmount = await _context.Payments
                .Where(p => p.Status == "Paid")
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;
            var awaitingTransferAmount = await _context.Payments
                .Where(p => p.Status == "AwaitingTransfer")
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            return Ok(new
            {
                totalEnterprises,
                totalCategories,
                totalProducts,
                approvedProducts,
                pendingProducts,
                rejectedProducts,
                totalApplications,
                pendingApplications,
                totalOrders,
                totalPayments,
                totalCustomers,
                totalEnterpriseAdmins,
                paidPaymentsAmount,
                awaitingTransferAmount
            });
        }

        /// <summary>
        /// Thống kê doanh nghiệp và sản phẩm OCOP theo huyện.
        /// </summary>
        [HttpGet("districts")]
        public async Task<ActionResult<IEnumerable<object>>> GetDistrictStats()
        {
            // ✅ PHIÊN BẢN PRODUCTION-READY - 100% có thể dịch sang SQL
            // Sử dụng SelectMany trực tiếp trên navigation property
            // EF Core tự động xử lý null bằng LEFT JOIN, không cần kiểm tra null thủ công
            // Sử dụng null-forgiving operator (!) vì EF Core đảm bảo navigation property được load khi query
            var stats = await _context.Enterprises
                .GroupBy(e => e.District ?? "Khác")
                .Select(g => new
                {
                    District = g.Key,
                    EnterpriseCount = g.Count(),
                    // ✅ SelectMany trực tiếp - EF Core dịch thành SQL JOIN
                    // ✅ Filter trong SelectMany - hoàn toàn có thể dịch sang SQL
                    // ✅ Sử dụng ! vì EF Core đảm bảo navigation property được xử lý đúng
                    ApprovedProducts = g.SelectMany(e => e.Products!)
                        .Where(p => p.Status == "Approved")
                        .Count(),
                    PendingProducts = g.SelectMany(e => e.Products!)
                        .Where(p => p.Status == "PendingApproval")
                        .Count()
                })
                .OrderByDescending(x => x.EnterpriseCount)
                .ToListAsync();

            return Ok(stats);
        }

        /// <summary>
        /// Doanh thu thanh toán đã duyệt theo tháng (12 tháng gần nhất).
        /// - SystemAdmin: Xem toàn hệ thống
        /// - EnterpriseAdmin: Chỉ xem doanh thu của doanh nghiệp mình
        /// </summary>
        [HttpGet("revenue-by-month")]
        public async Task<ActionResult<IEnumerable<object>>> GetRevenueByMonth()
        {
            try
            {
                // Kiểm tra quyền truy cập
                if (!_authorizationService.CanViewRevenueStatistics(User))
                {
                    return Forbid();
                }

                // Xác định enterpriseId được phép truy cập (null = toàn hệ thống)
                var authorizedEnterpriseId = await _authorizationService.GetAuthorizedEnterpriseIdAsync(
                    User,
                    null, // Không có enterpriseId từ request
                    HttpContext.RequestAborted);

                var toDate = DateTime.UtcNow;
                var fromDate = toDate.AddMonths(-11);

                // Query doanh thu với filter theo enterpriseId nếu có
                // ✅ Đảm bảo DateTime có Kind = UTC cho PostgreSQL
                var startDate = new DateTime(fromDate.Year, fromDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                
                var revenueQuery = _context.Payments
                    .Where(p => p.Status == "Paid" 
                        && p.PaidAt.HasValue 
                        && p.PaidAt.Value >= startDate);

                // Filter theo enterpriseId nếu user là EnterpriseAdmin
                if (authorizedEnterpriseId.HasValue)
                {
                    revenueQuery = revenueQuery.Where(p => p.EnterpriseId == authorizedEnterpriseId.Value);
                }

                var revenue = await revenueQuery
                    .GroupBy(p => new { p.PaidAt!.Value.Year, p.PaidAt.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Amount = g.Sum(p => p.Amount)
                    })
                    .OrderBy(g => g.Year).ThenBy(g => g.Month)
                    .ToListAsync();

                return Ok(revenue);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê doanh thu theo tháng", error = ex.Message });
            }
        }

        /// <summary>
        /// Thống kê doanh thu theo tuần/tháng/năm
        /// - SystemAdmin: Xem toàn hệ thống hoặc lọc theo enterpriseId
        /// - EnterpriseAdmin: Chỉ xem doanh thu của doanh nghiệp mình
        /// </summary>
        [HttpGet("revenue")]
        public async Task<ActionResult<RevenueStatisticsResponseDto>> GetRevenueStatistics(
            [FromQuery] string type = "month",
            [FromQuery] string? date = null,
            [FromQuery] int? enterpriseId = null)
        {
            try
            {
                var request = new RevenueStatisticsRequestDto
                {
                    Type = type,
                    Date = date,
                    EnterpriseId = enterpriseId
                };

                var response = await _revenueStatisticsService.GetRevenueStatisticsAsync(
                    request,
                    User,
                    HttpContext.RequestAborted);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính toán thống kê doanh thu. Type={Type}, Date={Date}, EnterpriseId={EnterpriseId}", 
                    type, date, enterpriseId);
                
                // Trả về thông tin lỗi chi tiết hơn
                var errorResponse = new 
                { 
                    message = "Lỗi khi tính toán thống kê doanh thu", 
                    error = ex.Message,
                    type = ex.GetType().Name
                };
                
                return StatusCode(500, errorResponse);
            }
        }
    }
}

