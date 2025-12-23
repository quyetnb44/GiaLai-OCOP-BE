using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GiaLaiOCOP.Api.Services.Revenue
{
    /// <summary>
    /// Implementation của IRevenueStatisticsService
    /// </summary>
    public class RevenueStatisticsService : IRevenueStatisticsService
    {
        private readonly IRevenueAuthorizationService _authorizationService;
        private readonly IRevenueCalculationService _calculationService;
        private readonly TimePeriodStrategyFactory _strategyFactory;
        private readonly AppDbContext _context;
        private readonly ILogger<RevenueStatisticsService> _logger;

        public RevenueStatisticsService(
            IRevenueAuthorizationService authorizationService,
            IRevenueCalculationService calculationService,
            TimePeriodStrategyFactory strategyFactory,
            AppDbContext context,
            ILogger<RevenueStatisticsService> logger)
        {
            _authorizationService = authorizationService;
            _calculationService = calculationService;
            _strategyFactory = strategyFactory;
            _context = context;
            _logger = logger;
        }

        public async Task<RevenueStatisticsResponseDto> GetRevenueStatisticsAsync(
            RevenueStatisticsRequestDto request,
            ClaimsPrincipal userClaims,
            CancellationToken cancellationToken = default)
        {
            // 1. Validate request
            if (!_strategyFactory.IsSupported(request.Type))
            {
                throw new ArgumentException($"Loại thời gian '{request.Type}' không được hỗ trợ. Chỉ hỗ trợ: week, month, year");
            }

            // 2. Kiểm tra quyền truy cập
            if (!_authorizationService.CanViewRevenueStatistics(userClaims))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem thống kê doanh thu");
            }

            // 3. Xác định enterpriseId được phép truy cập
            var authorizedEnterpriseId = await _authorizationService.GetAuthorizedEnterpriseIdAsync(
                userClaims,
                request.EnterpriseId,
                cancellationToken);

            // 4. Parse reference date
            DateTime referenceDate;
            if (!string.IsNullOrWhiteSpace(request.Date) && DateTime.TryParse(request.Date, out var parsedDate))
            {
                referenceDate = parsedDate.ToUniversalTime();
            }
            else
            {
                referenceDate = DateTime.UtcNow;
            }

            // 5. Lấy strategy phù hợp
            var strategy = _strategyFactory.GetStrategy(request.Type);

            // 6. Tính toán khoảng thời gian
            var period = strategy.CalculatePeriod(referenceDate);

            // 7. Tính doanh thu cho từng điểm dữ liệu trong chart
            var chartData = await _calculationService.CalculateChartDataAsync(
                strategy,
                referenceDate,
                authorizedEnterpriseId,
                cancellationToken);

            // 9. Tính tổng hợp
            var totalRevenue = await _calculationService.CalculateRevenueAsync(
                period.StartDate,
                period.EndDate,
                authorizedEnterpriseId,
                cancellationToken);

            var totalOrders = await _calculationService.CountOrdersAsync(
                period.StartDate,
                period.EndDate,
                authorizedEnterpriseId,
                cancellationToken);

            var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            // 10. Lấy tên doanh nghiệp nếu có
            string? enterpriseName = null;
            if (authorizedEnterpriseId.HasValue)
            {
                var enterprise = await _context.Enterprises
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == authorizedEnterpriseId.Value, cancellationToken);
                enterpriseName = enterprise?.Name;
            }

            // 11. Tạo response
            var response = new RevenueStatisticsResponseDto
            {
                Success = true,
                Filter = new RevenueStatisticsFilterDto
                {
                    Type = request.Type,
                    Date = referenceDate.ToString("yyyy-MM-dd"),
                    EnterpriseId = authorizedEnterpriseId,
                    EnterpriseName = enterpriseName
                },
                Summary = new RevenueStatisticsSummaryDto
                {
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    AverageOrderValue = averageOrderValue
                },
                Chart = chartData
            };

            _logger.LogInformation(
                "Thống kê doanh thu: Type={Type}, Date={Date}, EnterpriseId={EnterpriseId}, TotalRevenue={TotalRevenue}, TotalOrders={TotalOrders}",
                request.Type, referenceDate, authorizedEnterpriseId, totalRevenue, totalOrders);

            return response;
        }
    }
}

