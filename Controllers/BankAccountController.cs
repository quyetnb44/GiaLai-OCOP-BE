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
    public class BankAccountController : ControllerBase
    {
        private readonly IBankAccountService _bankAccountService;
        private readonly ILogger<BankAccountController> _logger;

        public BankAccountController(
            IBankAccountService bankAccountService,
            ILogger<BankAccountController> logger)
        {
            _bankAccountService = bankAccountService;
            _logger = logger;
        }

        // Helper: Lấy userId từ token
        private int? GetUserIdFromToken()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);
                var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                    return userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing token");
            }

            return null;
        }

        // Helper: Lấy role từ token
        private string? GetUserRoleFromToken()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);
                var roleClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                return roleClaim?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing token");
            }

            return null;
        }

        // POST: api/bankaccount - Tạo tài khoản ngân hàng mới
        // Customer/EnterpriseAdmin: chỉ có thể tạo cho chính mình
        // SystemAdmin: có thể tạo cho bất kỳ user nào
        [HttpPost]
        [ProducesResponseType(typeof(BankAccountDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<BankAccountDto>> CreateBankAccount([FromBody] CreateBankAccountDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var role = GetUserRoleFromToken();
            
            try
            {
                int targetUserId;
                
                if (role == "SystemAdmin")
                {
                    // SystemAdmin có thể tạo tài khoản cho bất kỳ user nào
                    // Nếu dto.UserId không có, tạo cho chính SystemAdmin
                    targetUserId = dto.UserId ?? userId.Value;
                }
                else
                {
                    // Customer/EnterpriseAdmin chỉ có thể tạo cho chính mình
                    // Bỏ qua dto.UserId nếu có và luôn tạo cho chính user đang đăng nhập
                    targetUserId = userId.Value;
                }
                
                var bankAccount = await _bankAccountService.CreateBankAccountAsync(targetUserId, dto);
                return CreatedAtAction(nameof(GetBankAccount), new { id = bankAccount.Id }, bankAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bank account for user: {UserId}", userId);
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/bankaccount - Lấy danh sách tài khoản ngân hàng của user (CHỈ XEM, KHÔNG SỬA)
        [HttpGet]
        [ProducesResponseType(typeof(List<BankAccountDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<List<BankAccountDto>>> GetUserBankAccounts()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            try
            {
                // User chỉ có thể xem tài khoản ngân hàng của chính mình
                var bankAccounts = await _bankAccountService.GetUserBankAccountsAsync(userId.Value);
                return Ok(bankAccounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bank accounts for user: {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách tài khoản ngân hàng." });
            }
        }

        // GET: api/bankaccount/default - Lấy tài khoản ngân hàng mặc định
        [HttpGet("default")]
        [ProducesResponseType(typeof(BankAccountDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<BankAccountDto>> GetDefaultBankAccount()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            try
            {
                var bankAccount = await _bankAccountService.GetDefaultBankAccountAsync(userId.Value);
                if (bankAccount == null)
                    return NotFound(new { message = "Chưa có tài khoản ngân hàng mặc định." });

                return Ok(bankAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default bank account for user: {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi khi lấy tài khoản ngân hàng mặc định." });
            }
        }

        // GET: api/bankaccount/{id} - Lấy chi tiết tài khoản ngân hàng (CHỈ XEM, KHÔNG SỬA)
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BankAccountDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<BankAccountDto>> GetBankAccount(int id)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var role = GetUserRoleFromToken();

            try
            {
                BankAccountDto? bankAccount;
                
                if (role == "SystemAdmin")
                {
                    // SystemAdmin có thể xem tài khoản của bất kỳ user nào
                    bankAccount = await _bankAccountService.GetBankAccountByIdForAdminAsync(id);
                }
                else
                {
                    // Customer/EnterpriseAdmin chỉ có thể xem tài khoản của chính mình
                    bankAccount = await _bankAccountService.GetBankAccountByIdAsync(id, userId.Value);
                }

                if (bankAccount == null)
                    return NotFound(new { message = "Tài khoản ngân hàng không tồn tại." });

                // Kiểm tra quyền: Customer/EnterpriseAdmin chỉ xem được tài khoản của mình
                if (role != "SystemAdmin" && bankAccount.UserId != userId.Value)
                {
                    return Forbid("Bạn không có quyền xem tài khoản ngân hàng này.");
                }

                return Ok(bankAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bank account: {BankAccountId}", id);
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin tài khoản ngân hàng." });
            }
        }

        // PUT: api/bankaccount/{id} - Cập nhật tài khoản ngân hàng
        // Customer/EnterpriseAdmin: chỉ có thể cập nhật tài khoản của chính mình
        // SystemAdmin: có thể cập nhật tài khoản của bất kỳ user nào
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(BankAccountDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<BankAccountDto>> UpdateBankAccount(int id, [FromBody] UpdateBankAccountDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var role = GetUserRoleFromToken();

            try
            {
                BankAccountDto? bankAccount;
                int targetUserId;
                
                if (role == "SystemAdmin")
                {
                    // SystemAdmin có thể cập nhật tài khoản của bất kỳ user nào
                    bankAccount = await _bankAccountService.GetBankAccountByIdForAdminAsync(id);
                    if (bankAccount == null)
                        return NotFound(new { message = "Tài khoản ngân hàng không tồn tại." });
                    targetUserId = bankAccount.UserId;
                }
                else
                {
                    // Customer/EnterpriseAdmin chỉ có thể cập nhật tài khoản của chính mình
                    bankAccount = await _bankAccountService.GetBankAccountByIdAsync(id, userId.Value);
                    if (bankAccount == null)
                        return NotFound(new { message = "Tài khoản ngân hàng không tồn tại." });
                    
                    // Kiểm tra quyền: chỉ có thể cập nhật tài khoản của chính mình
                    if (bankAccount.UserId != userId.Value)
                    {
                        return Forbid("Bạn không có quyền cập nhật tài khoản ngân hàng này.");
                    }
                    targetUserId = userId.Value;
                }
                
                var updatedAccount = await _bankAccountService.UpdateBankAccountAsync(id, targetUserId, dto);
                if (updatedAccount == null)
                    return NotFound(new { message = "Tài khoản ngân hàng không tồn tại." });
                
                return Ok(updatedAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank account: {BankAccountId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/bankaccount/{id} - Xóa tài khoản ngân hàng
        // Customer/EnterpriseAdmin: chỉ có thể xóa tài khoản của chính mình
        // SystemAdmin: có thể xóa tài khoản của bất kỳ user nào
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> DeleteBankAccount(int id)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var role = GetUserRoleFromToken();

            try
            {
                BankAccountDto? bankAccount;
                int targetUserId;
                
                if (role == "SystemAdmin")
                {
                    // SystemAdmin có thể xóa tài khoản của bất kỳ user nào
                    bankAccount = await _bankAccountService.GetBankAccountByIdForAdminAsync(id);
                    if (bankAccount == null)
                        return NotFound(new { message = "Tài khoản ngân hàng không tồn tại." });
                    targetUserId = bankAccount.UserId;
                }
                else
                {
                    // Customer/EnterpriseAdmin chỉ có thể xóa tài khoản của chính mình
                    bankAccount = await _bankAccountService.GetBankAccountByIdAsync(id, userId.Value);
                    if (bankAccount == null)
                        return NotFound(new { message = "Tài khoản ngân hàng không tồn tại." });
                    
                    // Kiểm tra quyền: chỉ có thể xóa tài khoản của chính mình
                    if (bankAccount.UserId != userId.Value)
                    {
                        return Forbid("Bạn không có quyền xóa tài khoản ngân hàng này.");
                    }
                    targetUserId = userId.Value;
                }

                var deleted = await _bankAccountService.DeleteBankAccountAsync(id, targetUserId);
                if (!deleted)
                    return NotFound(new { message = "Tài khoản ngân hàng không tồn tại." });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bank account: {BankAccountId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/bankaccount/{id}/set-default - Đặt làm tài khoản mặc định
        // Customer/EnterpriseAdmin: chỉ có thể đặt mặc định cho tài khoản của chính mình
        // SystemAdmin: có thể đặt mặc định cho tài khoản của bất kỳ user nào
        [HttpPost("{id}/set-default")]
        [ProducesResponseType(typeof(BankAccountDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<BankAccountDto>> SetDefaultBankAccount(int id)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var role = GetUserRoleFromToken();

            try
            {
                BankAccountDto? bankAccount;
                int targetUserId;
                
                if (role == "SystemAdmin")
                {
                    // SystemAdmin có thể đặt mặc định cho tài khoản của bất kỳ user nào
                    bankAccount = await _bankAccountService.GetBankAccountByIdForAdminAsync(id);
                    if (bankAccount == null)
                        return NotFound(new { message = "Tài khoản ngân hàng không tồn tại hoặc không hoạt động." });
                    targetUserId = bankAccount.UserId;
                }
                else
                {
                    // Customer/EnterpriseAdmin chỉ có thể đặt mặc định cho tài khoản của chính mình
                    bankAccount = await _bankAccountService.GetBankAccountByIdAsync(id, userId.Value);
                    if (bankAccount == null)
                        return NotFound(new { message = "Tài khoản ngân hàng không tồn tại hoặc không hoạt động." });
                    
                    // Kiểm tra quyền: chỉ có thể đặt mặc định cho tài khoản của chính mình
                    if (bankAccount.UserId != userId.Value)
                    {
                        return Forbid("Bạn không có quyền đặt mặc định cho tài khoản ngân hàng này.");
                    }
                    targetUserId = userId.Value;
                }

                var result = await _bankAccountService.SetDefaultBankAccountAsync(id, targetUserId);
                if (result == null)
                    return NotFound(new { message = "Tài khoản ngân hàng không tồn tại hoặc không hoạt động." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default bank account: {BankAccountId}", id);
                return StatusCode(500, new { message = "Lỗi khi đặt tài khoản mặc định." });
            }
        }
    }
}

