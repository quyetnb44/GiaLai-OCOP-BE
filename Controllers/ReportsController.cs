using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SystemAdmin")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
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
            var stats = await _context.Enterprises
                .GroupBy(e => e.District ?? "Khác")
                .Select(g => new
                {
                    District = g.Key,
                    EnterpriseCount = g.Count(),
                    ApprovedProducts = g.SelectMany(e => e.Products != null ? e.Products.Where(p => p.Status == "Approved") : Enumerable.Empty<Product>()).Count(),
                    PendingProducts = g.SelectMany(e => e.Products != null ? e.Products.Where(p => p.Status == "PendingApproval") : Enumerable.Empty<Product>()).Count()
                })
                .OrderByDescending(x => x.EnterpriseCount)
                .ToListAsync();

            return Ok(stats);
        }

        /// <summary>
        /// Doanh thu thanh toán đã duyệt theo tháng (12 tháng gần nhất).
        /// </summary>
        [HttpGet("revenue-by-month")]
        public async Task<ActionResult<IEnumerable<object>>> GetRevenueByMonth()
        {
            var toDate = DateTime.UtcNow;
            var fromDate = toDate.AddMonths(-11);

            var revenue = await _context.Payments
                .Where(p => p.Status == "Paid" && p.PaidAt.HasValue && p.PaidAt.Value >= new DateTime(fromDate.Year, fromDate.Month, 1))
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
    }
}

