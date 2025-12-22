using System.Security.Claims;

namespace GiaLaiOCOP.Api.Services
{
    /// <summary>
    /// Service kiểm soát phạm vi dữ liệu doanh thu dựa trên role của user
    /// </summary>
    public interface IRevenueAuthorizationService
    {
        /// <summary>
        /// Xác định enterpriseId mà user được phép truy cập
        /// </summary>
        /// <param name="userClaims">Claims của user từ token</param>
        /// <param name="requestedEnterpriseId">EnterpriseId từ request (chỉ SystemAdmin mới có thể filter)</param>
        /// <param name="dbContext">DbContext để query user info</param>
        /// <returns>EnterpriseId được phép truy cập (null = toàn hệ thống)</returns>
        Task<int?> GetAuthorizedEnterpriseIdAsync(
            ClaimsPrincipal userClaims,
            int? requestedEnterpriseId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra user có quyền xem thống kê doanh thu không
        /// </summary>
        bool CanViewRevenueStatistics(ClaimsPrincipal userClaims);
    }
}

