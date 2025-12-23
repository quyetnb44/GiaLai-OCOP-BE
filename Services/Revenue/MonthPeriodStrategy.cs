using GiaLaiOCOP.Api.Dtos;

namespace GiaLaiOCOP.Api.Services.Revenue
{
    /// <summary>
    /// Strategy cho thống kê theo tháng
    /// </summary>
    public class MonthPeriodStrategy : ITimePeriodStrategy
    {
        public string Type => "month";

        public TimePeriodResult CalculatePeriod(DateTime referenceDate)
        {
            // ✅ Đảm bảo DateTime có Kind = UTC cho PostgreSQL
            var startDate = new DateTime(referenceDate.Year, referenceDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1).AddTicks(-1);

            return new TimePeriodResult
            {
                StartDate = startDate,
                EndDate = endDate
            };
        }

        public List<RevenueStatisticsChartDto> GenerateChartDataPoints(DateTime referenceDate)
        {
            var chartData = new List<RevenueStatisticsChartDto>();
            var daysInMonth = DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month);

            // Tạo dữ liệu cho các ngày trong tháng
            for (int day = 1; day <= daysInMonth; day++)
            {
                // ✅ Đảm bảo DateTime có Kind = UTC cho PostgreSQL
                var dayStart = new DateTime(referenceDate.Year, referenceDate.Month, day, 0, 0, 0, DateTimeKind.Utc);
                chartData.Add(new RevenueStatisticsChartDto
                {
                    Label = dayStart.ToString("dd/MM"),
                    Revenue = 0 // Sẽ được tính toán sau
                });
            }

            return chartData;
        }
    }
}

