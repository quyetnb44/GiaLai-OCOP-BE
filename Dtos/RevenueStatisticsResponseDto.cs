namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO cho response thống kê doanh thu
    /// </summary>
    public class RevenueStatisticsResponseDto
    {
        public bool Success { get; set; }
        public RevenueStatisticsFilterDto Filter { get; set; } = null!;
        public RevenueStatisticsSummaryDto Summary { get; set; } = null!;
        public List<RevenueStatisticsChartDto> Chart { get; set; } = new();
    }

    /// <summary>
    /// Thông tin filter đã áp dụng
    /// </summary>
    public class RevenueStatisticsFilterDto
    {
        public string Type { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public int? EnterpriseId { get; set; }
        public string? EnterpriseName { get; set; }
    }

    /// <summary>
    /// Tổng hợp thống kê
    /// </summary>
    public class RevenueStatisticsSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    /// <summary>
    /// Dữ liệu cho biểu đồ
    /// </summary>
    public class RevenueStatisticsChartDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }
}

