using GiaLaiOCOP.Api.Dtos;

namespace GiaLaiOCOP.Api.Services.Revenue
{
    /// <summary>
    /// Strategy pattern cho các loại khoảng thời gian (tuần/tháng/năm)
    /// </summary>
    public interface ITimePeriodStrategy
    {
        /// <summary>
        /// Tên loại thời gian (week/month/year)
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Tính toán khoảng thời gian từ ngày tham chiếu
        /// </summary>
        TimePeriodResult CalculatePeriod(DateTime referenceDate);

        /// <summary>
        /// Tạo danh sách các điểm dữ liệu cho biểu đồ
        /// </summary>
        List<RevenueStatisticsChartDto> GenerateChartDataPoints(DateTime referenceDate);
    }

    /// <summary>
    /// Kết quả tính toán khoảng thời gian
    /// </summary>
    public class TimePeriodResult
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}

