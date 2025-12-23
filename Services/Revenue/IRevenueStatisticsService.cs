using GiaLaiOCOP.Api.Dtos;
using System.Security.Claims;

namespace GiaLaiOCOP.Api.Services.Revenue
{
    /// <summary>
    /// Service chính xử lý business logic cho thống kê doanh thu
    /// </summary>
    public interface IRevenueStatisticsService
    {
        /// <summary>
        /// Lấy thống kê doanh thu theo yêu cầu
        /// </summary>
        Task<RevenueStatisticsResponseDto> GetRevenueStatisticsAsync(
            RevenueStatisticsRequestDto request,
            ClaimsPrincipal userClaims,
            CancellationToken cancellationToken = default);
    }
}

