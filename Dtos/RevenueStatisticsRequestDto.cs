namespace GiaLaiOCOP.Api.Dtos
{
    /// <summary>
    /// DTO cho request thống kê doanh thu
    /// </summary>
    public class RevenueStatisticsRequestDto
    {
        /// <summary>
        /// Loại thời gian: "week", "month", "year"
        /// </summary>
        public string Type { get; set; } = "month";

        /// <summary>
        /// Ngày tham chiếu (optional, mặc định là ngày hiện tại)
        /// Format: yyyy-MM-dd
        /// </summary>
        public string? Date { get; set; }

        /// <summary>
        /// ID doanh nghiệp (chỉ SystemAdmin mới có thể filter theo enterpriseId)
        /// </summary>
        public int? EnterpriseId { get; set; }
    }
}

