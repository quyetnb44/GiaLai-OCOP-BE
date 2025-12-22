namespace GiaLaiOCOP.Api.Services.Revenue
{
    /// <summary>
    /// Service tính toán doanh thu
    /// </summary>
    public interface IRevenueCalculationService
    {
        /// <summary>
        /// Tính tổng doanh thu trong khoảng thời gian
        /// </summary>
        Task<decimal> CalculateRevenueAsync(
            DateTime startDate,
            DateTime endDate,
            int? enterpriseId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Đếm số đơn hàng trong khoảng thời gian
        /// </summary>
        Task<int> CountOrdersAsync(
            DateTime startDate,
            DateTime endDate,
            int? enterpriseId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tính doanh thu cho từng điểm dữ liệu trong chart
        /// </summary>
        Task<List<Dtos.RevenueStatisticsChartDto>> CalculateChartDataAsync(
            ITimePeriodStrategy strategy,
            DateTime referenceDate,
            int? enterpriseId,
            CancellationToken cancellationToken = default);
    }
}

