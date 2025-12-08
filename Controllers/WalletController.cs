using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Options;
using GiaLaiOCOP.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly AppDbContext _context;
        private readonly ILogger<WalletController> _logger;
        private readonly IOptions<BankTransferSettings> _bankSettings;

        public WalletController(
            IWalletService walletService,
            AppDbContext context,
            ILogger<WalletController> logger,
            IOptions<BankTransferSettings> bankSettings)
        {
            _walletService = walletService;
            _context = context;
            _logger = logger;
            _bankSettings = bankSettings;
        }

        // Helper: Lấy userId từ token
        private async Task<int?> GetUserIdFromTokenAsync()
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(claimValue))
                return null;

            if (int.TryParse(claimValue, out var userId))
                return userId;

            if (claimValue.Contains("@"))
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == claimValue);
                return user?.Id;
            }

            return null;
        }

        // Helper: Lấy role từ token
        private Task<string?> GetUserRoleFromTokenAsync()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim != null)
                return Task.FromResult<string?>(roleClaim.Value);

            // Fallback: Lấy từ JWT token nếu không có trong claims
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token))
                return Task.FromResult<string?>(null);

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);
                var roleClaimFromToken = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                return Task.FromResult<string?>(roleClaimFromToken?.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing token for role");
            }

            return Task.FromResult<string?>(null);
        }

        // GET: api/wallet - Xem số dư ví
        [HttpGet]
        [ProducesResponseType(typeof(WalletDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<WalletDto>> GetWallet()
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            try
            {
                var wallet = await _walletService.GetWalletAsync(userId.Value);
                return Ok(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet for user: {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin ví. Vui lòng thử lại." });
            }
        }

        // GET: api/wallet/transactions - Xem lịch sử giao dịch
        [HttpGet("transactions")]
        [ProducesResponseType(typeof(IEnumerable<WalletTransactionDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<WalletTransactionDto>>> GetTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            pageSize = pageSize > 100 ? 100 : pageSize;

            try
            {
                var transactions = await _walletService.GetTransactionsAsync(userId.Value, page, pageSize);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions for user: {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi khi lấy lịch sử giao dịch. Vui lòng thử lại." });
            }
        }

        // POST: api/wallet/deposit - Nạp tiền vào ví bằng VietQR
        [HttpPost("deposit")]
        [ProducesResponseType(typeof(DepositResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<DepositResponseDto>> Deposit([FromBody] DepositRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            try
            {
                var response = await _walletService.CreateDepositAsync(userId.Value, request);
                
                // Response đã có đầy đủ thông tin:
                // - PaymentUrl: Link ảnh QR code
                // - TransactionId: ID giao dịch
                // - Amount: Số tiền
                // - Description: Mô tả
                // - Reference: Mã tham chiếu để người dùng ghi chú khi chuyển khoản
                // Thông tin ngân hàng đã được encode trong QR code URL
                
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deposit for user: {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi khi tạo yêu cầu nạp tiền. Vui lòng thử lại." });
            }
        }

        // POST: api/wallet/pay - Thanh toán đơn hàng bằng ví
        [HttpPost("pay")]
        [ProducesResponseType(typeof(WalletTransactionDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<WalletTransactionDto>> PayOrder([FromBody] PayOrderRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            try
            {
                var transaction = await _walletService.PayOrderAsync(userId.Value, request);
                return Ok(transaction);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for order: {OrderId}", request.OrderId);
                return StatusCode(500, new { message = "Lỗi khi thanh toán đơn hàng. Vui lòng thử lại." });
            }
        }

        // POST: api/wallet/refund - Hoàn tiền
        [HttpPost("refund")]
        [ProducesResponseType(typeof(WalletTransactionDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<WalletTransactionDto>> Refund([FromBody] RefundRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            try
            {
                var transaction = await _walletService.RefundAsync(userId.Value, request);
                return Ok(transaction);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for order: {OrderId}", request.OrderId);
                return StatusCode(500, new { message = "Lỗi khi hoàn tiền. Vui lòng thử lại." });
            }
        }

        // POST: api/wallet/withdraw - Rút tiền từ ví
        [HttpPost("withdraw")]
        [ProducesResponseType(typeof(WalletTransactionDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<WalletTransactionDto>> Withdraw([FromBody] WithdrawRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            try
            {
                var transaction = await _walletService.WithdrawAsync(userId.Value, request);
                return Ok(transaction);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing withdraw for user: {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi khi rút tiền. Vui lòng thử lại." });
            }
        }

        // GET: api/wallet/system/summary - Tổng hợp số tiền hệ thống (SystemAdmin only)
        [HttpGet("system/summary")]
        [ProducesResponseType(typeof(SystemWalletSummaryDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<SystemWalletSummaryDto>> GetSystemWalletSummary()
        {
            var role = await GetUserRoleFromTokenAsync();
            if (role != "SystemAdmin")
            {
                return Forbid("Chỉ SystemAdmin mới có thể xem tổng hợp số tiền hệ thống.");
            }

            try
            {
                var summary = await _walletService.GetSystemWalletSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system wallet summary");
                return StatusCode(500, new { message = "Lỗi khi lấy tổng hợp số tiền hệ thống. Vui lòng thử lại." });
            }
        }

        // GET: api/wallet/system/users - Xem danh sách ví của tất cả User (SystemAdmin only)
        [HttpGet("system/users")]
        [ProducesResponseType(typeof(List<UserWalletSummaryDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<List<UserWalletSummaryDto>>> GetAllUserWallets(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var role = await GetUserRoleFromTokenAsync();
            if (role != "SystemAdmin")
            {
                return Forbid("Chỉ SystemAdmin mới có thể xem danh sách ví của tất cả User.");
            }

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            pageSize = pageSize > 100 ? 100 : pageSize;

            try
            {
                var wallets = await _walletService.GetAllUserWalletsAsync(page, pageSize);
                return Ok(wallets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all user wallets");
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách ví. Vui lòng thử lại." });
            }
        }

        // POST: api/wallet/system/ensure-all-wallets - Đảm bảo tất cả user đều có ví (SystemAdmin only)
        [HttpPost("system/ensure-all-wallets")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult> EnsureAllUsersHaveWallets()
        {
            var role = await GetUserRoleFromTokenAsync();
            if (role != "SystemAdmin")
            {
                return Forbid("Chỉ SystemAdmin mới có thể chạy script này.");
            }

            try
            {
                var count = await _walletService.EnsureAllUsersHaveWalletsAsync();
                return Ok(new { 
                    message = $"Đã tạo ví cho {count} user chưa có ví.",
                    createdWalletsCount = count 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring all users have wallets");
                return StatusCode(500, new { message = "Lỗi khi tạo ví cho user. Vui lòng thử lại." });
            }
        }

        // GET: api/wallet/user/{userId} - Xem ví của user cụ thể (SystemAdmin only)
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(WalletDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<WalletDto>> GetUserWallet(int userId)
        {
            var role = await GetUserRoleFromTokenAsync();
            if (role != "SystemAdmin")
            {
                return Forbid("Chỉ SystemAdmin mới có thể xem ví của user khác.");
            }

            try
            {
                var wallet = await _walletService.GetUserWalletAsync(userId);
                return Ok(wallet);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user wallet: UserId={UserId}", userId);
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin ví. Vui lòng thử lại." });
            }
        }

        // PUT: api/wallet/user/{userId}/balance - Cập nhật số dư ví của user (SystemAdmin only)
        [HttpPut("user/{userId}/balance")]
        [ProducesResponseType(typeof(WalletTransactionDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<WalletTransactionDto>> UpdateUserWalletBalance(
            int userId,
            [FromBody] UpdateWalletBalanceDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminUserId = await GetUserIdFromTokenAsync();
            if (adminUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var role = await GetUserRoleFromTokenAsync();
            if (role != "SystemAdmin")
            {
                return Forbid("Chỉ SystemAdmin mới có thể cập nhật số dư ví của user.");
            }

            try
            {
                var transaction = await _walletService.UpdateUserWalletBalanceAsync(
                    userId, 
                    request.Amount, 
                    request.Description, 
                    adminUserId.Value);

                var message = request.Amount >= 0
                    ? $"Đã cộng {request.Amount:N0} VND vào ví của user."
                    : $"Đã trừ {Math.Abs(request.Amount):N0} VND từ ví của user.";

                return Ok(new { 
                    message = message,
                    transaction = transaction 
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating wallet balance: UserId={UserId}, Amount={Amount}", userId, request.Amount);
                return StatusCode(500, new { message = "Lỗi khi cập nhật số dư ví. Vui lòng thử lại." });
            }
        }
    }
}

