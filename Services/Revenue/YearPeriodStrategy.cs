using GiaLaiOCOP.Api.Dtos;

namespace GiaLaiOCOP.Api.Services.Revenue
{
    /// <summary>
    /// Strategy cho thống kê theo năm
    /// </summary>
    public class YearPeriodStrategy : ITimePeriodStrategy
    {
        public string Type => "year";

        public TimePeriodResult CalculatePeriod(DateTime referenceDate)
        {
            // ✅ Đảm bảo DateTime có Kind = UTC cho PostgreSQL
            var startDate = new DateTime(referenceDate.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(referenceDate.Year, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);

            return new TimePeriodResult
            {
                StartDate = startDate,
                EndDate = endDate
            };
        }

        public List<RevenueStatisticsChartDto> GenerateChartDataPoints(DateTime referenceDate)
        {
            var chartData = new List<RevenueStatisticsChartDto>();

            // Tạo dữ liệu cho 12 tháng trong năm
            for (int month = 1; month <= 12; month++)
            {
                // ✅ Đảm bảo DateTime có Kind = UTC cho PostgreSQL
                var monthStart = new DateTime(referenceDate.Year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                chartData.Add(new RevenueStatisticsChartDto
                {
                    Label = monthStart.ToString("MM/yyyy"),
                    Revenue = 0 // Sẽ được tính toán sau
                });
            }

            return chartData;
        }
    }
}

