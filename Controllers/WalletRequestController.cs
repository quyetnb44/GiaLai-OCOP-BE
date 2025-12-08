using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletRequestController : ControllerBase
    {
        private readonly IWalletRequestService _walletRequestService;
        private readonly ILogger<WalletRequestController> _logger;

        public WalletRequestController(
            IWalletRequestService walletRequestService,
            ILogger<WalletRequestController> logger)
        {
            _walletRequestService = walletRequestService;
            _logger = logger;
        }

        // Helper: Lấy userId từ token
        private Task<int?> GetUserIdFromTokenAsync()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token))
                return Task.FromResult<int?>(null);

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);
                var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                    return Task.FromResult<int?>(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing token");
            }

            return Task.FromResult<int?>(null);
        }

        // Helper: Lấy role từ token
        private Task<string?> GetUserRoleFromTokenAsync()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token))
                return Task.FromResult<string?>(null);

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);
                var roleClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                return Task.FromResult<string?>(roleClaim?.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing token");
            }

            return Task.FromResult<string?>(null);
        }

        // POST: api/walletrequest - Tạo yêu cầu nạp/rút tiền (Customer, EnterpriseAdmin)
        [HttpPost]
        [ProducesResponseType(typeof(WalletRequestDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<WalletRequestDto>> CreateRequest([FromBody] CreateWalletRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var role = await GetUserRoleFromTokenAsync();
            if (role != "Customer" && role != "EnterpriseAdmin")
            {
                return Forbid("Chỉ Customer và EnterpriseAdmin mới có thể tạo yêu cầu nạp/rút tiền.");
            }

            try
            {
                var walletRequest = await _walletRequestService.CreateRequestAsync(userId.Value, request);
                return Ok(walletRequest);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wallet request for user: {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi khi tạo yêu cầu. Vui lòng thử lại." });
            }
        }

        // GET: api/walletrequest - Lấy danh sách yêu cầu
        [HttpGet]
        [ProducesResponseType(typeof(List<WalletRequestDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<List<WalletRequestDto>>> GetRequests(
            [FromQuery] string? type = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var role = await GetUserRoleFromTokenAsync();

            // SystemAdmin có thể xem tất cả, Customer/EnterpriseAdmin chỉ xem của mình
            int? filterUserId = role == "SystemAdmin" ? null : userId;

            try
            {
                var requests = await _walletRequestService.GetRequestsAsync(filterUserId, type, status, page, pageSize);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet requests");
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách yêu cầu. Vui lòng thử lại." });
            }
        }

        // GET: api/walletrequest/{id} - Lấy chi tiết yêu cầu
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(WalletRequestDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<WalletRequestDto>> GetRequest(int id)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var role = await GetUserRoleFromTokenAsync();

            try
            {
                var request = await _walletRequestService.GetRequestByIdAsync(id);
                if (request == null)
                {
                    return NotFound(new { message = "Yêu cầu không tồn tại." });
                }

                // Kiểm tra quyền: SystemAdmin xem được tất cả, Customer/EnterpriseAdmin chỉ xem của mình
                if (role != "SystemAdmin" && request.UserId != userId.Value)
                {
                    return Forbid("Bạn không có quyền xem yêu cầu này.");
                }

                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet request: {RequestId}", id);
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin yêu cầu. Vui lòng thử lại." });
            }
        }

        // POST: api/walletrequest/{id}/process - Xử lý yêu cầu (CHỈ SystemAdmin)
        // SystemAdmin sẽ xem thông tin yêu cầu, nếu hợp lệ sẽ chuyển khoản thủ công cho người tạo yêu cầu
        // Sau đó SystemAdmin phê duyệt yêu cầu, hệ thống sẽ tự động cộng/trừ tiền vào ví
        [HttpPost("{id}/process")]
        [ProducesResponseType(typeof(WalletRequestDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<WalletRequestDto>> ProcessRequest(
            int id,
            [FromBody] ProcessWalletRequestDto processRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminUserId = await GetUserIdFromTokenAsync();
            if (adminUserId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            // CHỈ SystemAdmin mới có quyền xét duyệt yêu cầu
            var role = await GetUserRoleFromTokenAsync();
            if (role != "SystemAdmin")
            {
                return Forbid("Chỉ SystemAdmin mới có quyền xét duyệt yêu cầu nạp/rút tiền.");
            }

            try
            {
                // Xử lý yêu cầu:
                // - Nếu approve: Cộng/trừ tiền vào ví của người tạo yêu cầu
                // - Nếu reject: Chỉ cập nhật trạng thái, không thay đổi số dư
                var request = await _walletRequestService.ProcessRequestAsync(id, adminUserId.Value, processRequest);
                
                var message = processRequest.Action == "approve"
                    ? "Yêu cầu đã được phê duyệt. Số tiền đã được cập nhật vào ví."
                    : "Yêu cầu đã bị từ chối.";

                return Ok(new { 
                    message = message,
                    request = request 
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing wallet request: {RequestId}, AdminId={AdminId}", id, adminUserId);
                return StatusCode(500, new { message = "Lỗi khi xử lý yêu cầu. Vui lòng thử lại." });
            }
        }

        // GET: api/walletrequest/pending/count - Lấy số lượng yêu cầu đang chờ (SystemAdmin only)
        [HttpGet("pending/count")]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<int>> GetPendingRequestsCount()
        {
            var role = await GetUserRoleFromTokenAsync();
            if (role != "SystemAdmin")
            {
                return Forbid("Chỉ SystemAdmin mới có thể xem số lượng yêu cầu đang chờ.");
            }

            try
            {
                var count = await _walletRequestService.GetPendingRequestsCountAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending requests count");
                return StatusCode(500, new { message = "Lỗi khi lấy số lượng yêu cầu đang chờ." });
            }
        }
    }
}

