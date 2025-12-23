using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace GiaLaiOCOP.Api.Services.Revenue
{
    /// <summary>
    /// Implementation của IRevenueCalculationService
    /// </summary>
    public class RevenueCalculationService : IRevenueCalculationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RevenueCalculationService> _logger;

        public RevenueCalculationService(
            AppDbContext context,
            ILogger<RevenueCalculationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<decimal> CalculateRevenueAsync(
            DateTime startDate,
            DateTime endDate,
            int? enterpriseId,
            CancellationToken cancellationToken = default)
        {
            // Nếu có enterpriseId, tính từ OrderItems có Product thuộc enterprise đó
            if (enterpriseId.HasValue)
            {
                try
                {
                    var revenue = await _context.OrderItems
                        .Include(oi => oi.Order)
                        .Include(oi => oi.Product)
                        .Where(oi => oi.Order != null
                            && oi.Order.Status == "Completed"
                            && oi.Order.OrderDate >= startDate
                            && oi.Order.OrderDate <= endDate
                            && oi.Product != null
                            && oi.Product.EnterpriseId == enterpriseId.Value)
                        .SumAsync(oi => (decimal?)(oi.Quantity * oi.Price), cancellationToken) ?? 0m;

                    _logger.LogDebug("Tính doanh thu cho EnterpriseId {EnterpriseId}: {Revenue} từ {StartDate} đến {EndDate}",
                        enterpriseId.Value, revenue, startDate, endDate);
                    return revenue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi tính doanh thu cho EnterpriseId {EnterpriseId} từ {StartDate} đến {EndDate}",
                        enterpriseId.Value, startDate, endDate);
                    throw;
                }
            }
            else
            {
                // Không filter enterprise: tính tổng TotalAmount của các đơn hàng
                var revenue = await _context.Orders
                    .Where(o => o.Status == "Completed"
                        && o.OrderDate >= startDate
                        && o.OrderDate <= endDate)
                    .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

                _logger.LogDebug("Tính doanh thu toàn hệ thống: {Revenue} từ {StartDate} đến {EndDate}",
                    revenue, startDate, endDate);
                return revenue;
            }
        }

        public async Task<int> CountOrdersAsync(
            DateTime startDate,
            DateTime endDate,
            int? enterpriseId,
            CancellationToken cancellationToken = default)
        {
            if (enterpriseId.HasValue)
            {
                // Đếm số đơn hàng unique có OrderItems thuộc enterprise
                try
                {
                    var orderCount = await _context.OrderItems
                        .Include(oi => oi.Order)
                        .Include(oi => oi.Product)
                        .Where(oi => oi.Order != null
                            && oi.Order.Status == "Completed"
                            && oi.Order.OrderDate >= startDate
                            && oi.Order.OrderDate <= endDate
                            && oi.Product != null
                            && oi.Product.EnterpriseId == enterpriseId.Value)
                        .Select(oi => oi.OrderId)
                        .Distinct()
                        .CountAsync(cancellationToken);

                    _logger.LogDebug("Đếm đơn hàng cho EnterpriseId {EnterpriseId}: {Count} từ {StartDate} đến {EndDate}",
                        enterpriseId.Value, orderCount, startDate, endDate);
                    return orderCount;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi đếm đơn hàng cho EnterpriseId {EnterpriseId} từ {StartDate} đến {EndDate}",
                        enterpriseId.Value, startDate, endDate);
                    throw;
                }
            }
            else
            {
                var orderCount = await _context.Orders
                    .Where(o => o.Status == "Completed"
                        && o.OrderDate >= startDate
                        && o.OrderDate <= endDate)
                    .CountAsync(cancellationToken);

                _logger.LogDebug("Đếm đơn hàng toàn hệ thống: {Count} từ {StartDate} đến {EndDate}",
                    orderCount, startDate, endDate);
                return orderCount;
            }
        }

        public async Task<List<RevenueStatisticsChartDto>> CalculateChartDataAsync(
            ITimePeriodStrategy strategy,
            DateTime referenceDate,
            int? enterpriseId,
            CancellationToken cancellationToken = default)
        {
            var result = new List<RevenueStatisticsChartDto>();
            var period = strategy.CalculatePeriod(referenceDate);

            if (strategy.Type == "week")
            {
                // Tính doanh thu cho từng ngày trong tuần
                for (int i = 0; i < 7; i++)
                {
                    var dayStart = period.StartDate.AddDays(i);
                    var dayEnd = dayStart.AddDays(1).AddTicks(-1);
                    var revenue = await CalculateRevenueAsync(dayStart, dayEnd, enterpriseId, cancellationToken);
                    
                    result.Add(new RevenueStatisticsChartDto
                    {
                        Label = dayStart.ToString("dd/MM"),
                        Revenue = revenue
                    });
                }
            }
            else if (strategy.Type == "month")
            {
                // Tính doanh thu cho từng ngày trong tháng
                var daysInMonth = DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month);
                for (int day = 1; day <= daysInMonth; day++)
                {
                    // ✅ Đảm bảo DateTime có Kind = UTC cho PostgreSQL
                    var dayStart = new DateTime(referenceDate.Year, referenceDate.Month, day, 0, 0, 0, DateTimeKind.Utc);
                    var dayEnd = dayStart.AddDays(1).AddTicks(-1);
                    var revenue = await CalculateRevenueAsync(dayStart, dayEnd, enterpriseId, cancellationToken);
                    
                    result.Add(new RevenueStatisticsChartDto
                    {
                        Label = dayStart.ToString("dd/MM"),
                        Revenue = revenue
                    });
                }
            }
            else if (strategy.Type == "year")
            {
                // Tính doanh thu cho từng tháng trong năm
                for (int month = 1; month <= 12; month++)
                {
                    // ✅ Đảm bảo DateTime có Kind = UTC cho PostgreSQL
                    var monthStart = new DateTime(referenceDate.Year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                    var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
                    var revenue = await CalculateRevenueAsync(monthStart, monthEnd, enterpriseId, cancellationToken);
                    
                    result.Add(new RevenueStatisticsChartDto
                    {
                        Label = monthStart.ToString("MM/yyyy"),
                        Revenue = revenue
                    });
                }
            }
            else
            {
                _logger.LogWarning("Strategy type '{Type}' không được hỗ trợ", strategy.Type);
                return new List<RevenueStatisticsChartDto>(); // Trả về list rỗng nếu không hỗ trợ
            }

            return result;
        }
    }
}

