using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace GiaLaiOCOP.Api.Services
{
    /// <summary>
    /// Implementation của IRevenueAuthorizationService
    /// </summary>
    public class RevenueAuthorizationService : IRevenueAuthorizationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RevenueAuthorizationService> _logger;

        public RevenueAuthorizationService(
            AppDbContext context,
            ILogger<RevenueAuthorizationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public bool CanViewRevenueStatistics(ClaimsPrincipal userClaims)
        {
            var role = userClaims.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(role))
                return false;
            
            // Case-insensitive comparison
            var roleLower = role.ToLower().Trim();
            return roleLower == "systemadmin" || roleLower == "enterpriseadmin";
        }

        public async Task<int?> GetAuthorizedEnterpriseIdAsync(
            ClaimsPrincipal userClaims,
            int? requestedEnterpriseId,
            CancellationToken cancellationToken = default)
        {
            var role = userClaims.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(role))
            {
                _logger.LogWarning("User không có role claim");
                throw new UnauthorizedAccessException("Không tìm thấy role trong token");
            }

            // Normalize role for comparison (case-insensitive)
            var roleNormalized = role.Trim();

            // EnterpriseAdmin: Chỉ được xem doanh thu của doanh nghiệp mình
            if (roleNormalized.Equals("EnterpriseAdmin", StringComparison.OrdinalIgnoreCase))
            {
                var userIdClaim = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                 ?? userClaims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim))
                {
                    _logger.LogWarning("EnterpriseAdmin không có userId claim");
                    throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng");
                }

                // 🔹 Xử lý cả trường hợp userId là số hoặc email
                User? user = null;
                
                if (int.TryParse(userIdClaim, out var userId))
                {
                    // Nếu là số, tìm user theo Id
                    user = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                }
                else if (userIdClaim.Contains("@"))
                {
                    // Nếu là email, tìm user theo email
                    user = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Email == userIdClaim, cancellationToken);
                }

                if (user == null)
                {
                    _logger.LogWarning("EnterpriseAdmin không tìm thấy user với claim: {Claim}", userIdClaim);
                    throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng");
                }

                if (user.EnterpriseId == null)
                {
                    _logger.LogWarning("EnterpriseAdmin {UserId} không có EnterpriseId", user.Id);
                    throw new UnauthorizedAccessException("Không tìm thấy doanh nghiệp của bạn");
                }

                // EnterpriseAdmin KHÔNG được filter theo enterpriseId khác
                // Luôn trả về enterpriseId của chính họ
                _logger.LogInformation("EnterpriseAdmin {UserId} truy cập doanh thu của EnterpriseId {EnterpriseId}", 
                    user.Id, user.EnterpriseId);
                return user.EnterpriseId;
            }

            // SystemAdmin: Có thể xem toàn hệ thống hoặc filter theo enterpriseId
            if (roleNormalized.Equals("SystemAdmin", StringComparison.OrdinalIgnoreCase))
            {
                // Nếu có requestedEnterpriseId, validate nó tồn tại
                if (requestedEnterpriseId.HasValue)
                {
                    var enterpriseExists = await _context.Enterprises
                        .AsNoTracking()
                        .AnyAsync(e => e.Id == requestedEnterpriseId.Value, cancellationToken);

                    if (!enterpriseExists)
                    {
                        _logger.LogWarning("SystemAdmin yêu cầu EnterpriseId {EnterpriseId} không tồn tại", 
                            requestedEnterpriseId.Value);
                        throw new ArgumentException($"Doanh nghiệp với ID {requestedEnterpriseId.Value} không tồn tại");
                    }
                }

                _logger.LogInformation("SystemAdmin truy cập doanh thu với EnterpriseId filter: {EnterpriseId}", 
                    requestedEnterpriseId);
                return requestedEnterpriseId; // null = toàn hệ thống
            }

            // Role khác không được phép
            _logger.LogWarning("User với role {Role} không được phép xem thống kê doanh thu", roleNormalized);
            throw new UnauthorizedAccessException($"Chỉ SystemAdmin và EnterpriseAdmin mới có quyền xem thống kê doanh thu. Role hiện tại: {roleNormalized}");
        }
    }
}

