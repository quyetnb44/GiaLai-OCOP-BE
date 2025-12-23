using GiaLaiOCOP.Api.Dtos;

namespace GiaLaiOCOP.Api.Services.Revenue
{
    /// <summary>
    /// Strategy cho thống kê theo tuần (7 ngày, bắt đầu từ thứ 2)
    /// </summary>
    public class WeekPeriodStrategy : ITimePeriodStrategy
    {
        public string Type => "week";

        public TimePeriodResult CalculatePeriod(DateTime referenceDate)
        {
            // Tuần bắt đầu từ thứ 2
            var dayOfWeek = (int)referenceDate.DayOfWeek;
            var daysToSubtract = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Chủ nhật = 0, cần trừ 6 ngày
            // ✅ Đảm bảo DateTime có Kind = UTC cho PostgreSQL
            var startDate = referenceDate.Date.AddDays(-daysToSubtract);
            if (startDate.Kind != DateTimeKind.Utc)
            {
                startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            }
            var endDate = startDate.AddDays(7).AddTicks(-1);

            return new TimePeriodResult
            {
                StartDate = startDate,
                EndDate = endDate
            };
        }

        public List<RevenueStatisticsChartDto> GenerateChartDataPoints(DateTime referenceDate)
        {
            var period = CalculatePeriod(referenceDate);
            var chartData = new List<RevenueStatisticsChartDto>();

            // Tạo dữ liệu cho 7 ngày trong tuần
            for (int i = 0; i < 7; i++)
            {
                var dayStart = period.StartDate.AddDays(i);
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

