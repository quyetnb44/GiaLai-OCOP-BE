using System;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Models;
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
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IOptions<BankTransferSettings> _bankOptions;
        private readonly IVietQrService _vietQrService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            AppDbContext context, 
            IOptions<BankTransferSettings> bankOptions,
            IVietQrService vietQrService,
            ILogger<PaymentsController> logger)
        {
            _context = context;
            _bankOptions = bankOptions;
            _vietQrService = vietQrService;
            _logger = logger;
        }

        // 🔹 Helper: Lấy userId từ token
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

        // 🔹 POST: api/payments
        // Customer tạo thanh toán cho đơn hàng của mình
        // Tự động tạo payment riêng cho mỗi enterprise trong đơn hàng
        [HttpPost]
        [ProducesResponseType(typeof(IEnumerable<PaymentDto>), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> CreatePayment([FromBody] CreatePaymentRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var order = await _context.Orders
                .Include(o => o.Payments)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Enterprise)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "Customer" && order.UserId != userId.Value)
                return Forbid("Bạn chỉ có thể tạo thanh toán cho đơn hàng của chính mình.");

            if (role == "EnterpriseAdmin")
                return Forbid("EnterpriseAdmin không thể tạo thanh toán cho khách hàng.");

            var method = NormalizeMethod(dto.Method);
            if (method != "COD" && method != "BankTransfer")
                return BadRequest("Phương thức thanh toán không hợp lệ. Chỉ chấp nhận: COD, BankTransfer.");

            // Hủy tất cả payments pending/awaiting của order này
            var existingPayments = order.Payments
                .Where(p => p.Status == "Pending" || p.Status == "AwaitingTransfer")
                .ToList();

            foreach (var existingPayment in existingPayments)
            {
                existingPayment.Status = "Cancelled";
                existingPayment.Notes = "Tự động hủy do tạo phương thức thanh toán mới.";
            }

            // Đảm bảo OrderItems và Product được load
            if (order.OrderItems == null || !order.OrderItems.Any())
            {
                // Load OrderItems nếu chưa có
                await _context.Entry(order).Collection(o => o.OrderItems).LoadAsync();
                
                // Load Product cho mỗi OrderItem
                if (order.OrderItems != null)
                {
                    foreach (var item in order.OrderItems)
                    {
                        await _context.Entry(item).Reference(oi => oi.Product).LoadAsync();
                    }
                }
            }

            // Nhóm OrderItems theo EnterpriseId và tính amount cho mỗi enterprise
            if (order.OrderItems == null)
                return BadRequest("Đơn hàng không có sản phẩm nào.");

            var enterpriseGroups = order.OrderItems
                .Where(oi => oi.Product != null && oi.Product.EnterpriseId > 0)
                .GroupBy(oi => oi.Product!.EnterpriseId)
                .ToList();

            if (!enterpriseGroups.Any())
                return BadRequest("Đơn hàng không có sản phẩm hợp lệ hoặc sản phẩm không thuộc Enterprise nào.");

            var createdPayments = new List<Payment>();

            foreach (var group in enterpriseGroups)
            {
                var enterpriseId = group.Key;
                var enterprise = await _context.Enterprises.FindAsync(enterpriseId);
                if (enterprise == null)
                {
                    // Log warning nhưng tiếp tục với enterprise khác
                    continue;
                }

                // Tính tổng amount cho enterprise này
                var amount = group.Sum(oi => oi.Price * oi.Quantity);
                
                if (amount <= 0)
                    continue;

                // Tạo payment cho enterprise này
                try
                {
                    var payment = await CreatePaymentForEnterpriseAsync(order, enterprise, amount, method);
                    createdPayments.Add(payment);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new { Error = ex.Message });
                }
            }

            if (!createdPayments.Any())
                return BadRequest("Không thể tạo payment. Vui lòng kiểm tra lại thông tin đơn hàng và Enterprise.");

            // 🔹 Cập nhật Order.PaymentStatus dựa trên tất cả payments
            var allBankTransfer = createdPayments.All(p => p.Method == "BankTransfer");
            var allCOD = createdPayments.All(p => p.Method == "COD");
            
            if (allBankTransfer)
            {
                order.PaymentStatus = "AwaitingTransfer";
            }
            else if (allCOD)
            {
                order.PaymentStatus = "Pending";
            }
            else
            {
                // Có cả BankTransfer và COD → Ưu tiên BankTransfer
                order.PaymentStatus = "AwaitingTransfer";
            }

            await _context.SaveChangesAsync();

            // Load Enterprise để map vào DTO
            foreach (var payment in createdPayments)
            {
                await _context.Entry(payment)
                    .Reference(p => p.Enterprise)
                    .LoadAsync();
            }

            var paymentDtos = createdPayments.Select(MapPaymentToDto).ToList();

            return CreatedAtAction(nameof(GetPaymentsByOrder), new { orderId = order.Id }, paymentDtos);
        }

        // 🔹 GET: api/payments/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PaymentDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PaymentDto>> GetPayment(int id)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var payment = await _context.Payments
                .Include(p => p.Order)
                .Include(p => p.Enterprise)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound("Không tìm thấy thanh toán.");

            if (!CanAccessPayment(payment, userId.Value))
                return Forbid("Bạn không có quyền xem thanh toán này.");

            return Ok(MapPaymentToDto(payment));
        }

        // 🔹 GET: api/payments/{id}/qr-code - Lấy QR code thanh toán với amount và description
        [HttpGet("{id}/qr-code")]
        [ProducesResponseType(typeof(PaymentQrCodeDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PaymentQrCodeDto>> GetPaymentQrCode(int id)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var payment = await _context.Payments
                .Include(p => p.Order)
                .Include(p => p.Enterprise)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound("Không tìm thấy thanh toán.");

            if (!CanAccessPayment(payment, userId.Value))
                return Forbid("Bạn không có quyền xem QR code thanh toán này.");

            if (payment.Method != "BankTransfer")
                return BadRequest("Chỉ có thể lấy QR code cho thanh toán chuyển khoản.");

            // Lấy thông tin ngân hàng từ EnterpriseBankInfo
            var bankInfo = await _context.EnterpriseBankInfos
                .FirstOrDefaultAsync(ebi => ebi.EnterpriseId == payment.EnterpriseId);

            if (bankInfo == null)
            {
                // Fallback: Sử dụng thông tin từ Payment hoặc Enterprise (tương thích với code cũ)
                if (string.IsNullOrWhiteSpace(payment.BankCode) || 
                    string.IsNullOrWhiteSpace(payment.BankAccount) || 
                    string.IsNullOrWhiteSpace(payment.AccountName))
                {
                    return NotFound("Enterprise chưa cấu hình thông tin ngân hàng.");
                }

                // Tạo QR code từ thông tin Payment (fallback)
                try
                {
                    var description = $"Thanh toan don hang #{payment.OrderId}";
                    var qrCodeBase64 = _vietQrService.GeneratePaymentQrCodeBase64(
                        payment.BankCode!,
                        payment.BankAccount!,
                        payment.AccountName!,
                        payment.Amount,
                        description
                    );

                    return Ok(new PaymentQrCodeDto
                    {
                        QrCodeBase64 = qrCodeBase64,
                        Description = description,
                        Amount = payment.Amount,
                        EnterpriseBankName = payment.Enterprise?.Name ?? "Unknown",
                        EnterpriseAccountNumber = payment.BankAccount!,
                        AccountName = payment.AccountName!
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating payment QR code for Payment {PaymentId}", id);
                    return StatusCode(500, new { message = "Lỗi khi tạo QR code. Vui lòng thử lại." });
                }
            }

            // Tạo QR code từ EnterpriseBankInfo
            try
            {
                var description = $"Thanh toan don hang #{payment.OrderId}";
                var qrCodeBase64 = _vietQrService.GeneratePaymentQrCodeBase64(
                    bankInfo.BankCode,
                    bankInfo.BankAccount,
                    bankInfo.AccountName,
                    payment.Amount,
                    description
                );

                return Ok(new PaymentQrCodeDto
                {
                    QrCodeBase64 = qrCodeBase64,
                    Description = description,
                    Amount = payment.Amount,
                    EnterpriseBankName = bankInfo.BankName,
                    EnterpriseAccountNumber = bankInfo.BankAccount,
                    AccountName = bankInfo.AccountName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payment QR code for Payment {PaymentId}", id);
                return StatusCode(500, new { message = "Lỗi khi tạo QR code. Vui lòng thử lại." });
            }
        }

        // 🔹 GET: api/payments/order/{orderId}
        [HttpGet("order/{orderId}")]
        [ProducesResponseType(typeof(IEnumerable<PaymentDto>), 200)]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPaymentsByOrder(int orderId)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var order = await _context.Orders
                .Include(o => o.Payments)
                    .ThenInclude(p => p.Enterprise)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            if (!CanAccessOrder(order, userId.Value))
                return Forbid("Bạn không có quyền xem thanh toán của đơn hàng này.");

            var payments = order.Payments
                .OrderByDescending(p => p.CreatedAt)
                .Select(MapPaymentToDto)
                .ToList();

            return Ok(payments);
        }

        // 🔹 POST: api/payments/{id}/status
        // SystemAdmin / EnterpriseAdmin xác nhận thanh toán
        [HttpPost("{id}/status")]
        [Authorize(Roles = "SystemAdmin,EnterpriseAdmin")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var payment = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Payments)
                .Include(p => p.Enterprise)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound("Không tìm thấy thanh toán.");

            // Kiểm tra quyền: EnterpriseAdmin chỉ có thể cập nhật payment của enterprise của mình
            if (User.IsInRole("EnterpriseAdmin") && !User.IsInRole("SystemAdmin"))
            {
                var userEnterpriseId = await _context.Users
                    .Where(u => u.Id == userId.Value)
                    .Select(u => u.EnterpriseId)
                    .FirstOrDefaultAsync();

                if (userEnterpriseId == null || userEnterpriseId != payment.EnterpriseId)
                    return Forbid("Bạn chỉ có thể cập nhật thanh toán của doanh nghiệp của mình.");
            }

            var newStatus = NormalizeStatus(dto.Status);
            if (newStatus != "Paid" && newStatus != "Cancelled")
                return BadRequest("Trạng thái không hợp lệ. Chỉ chấp nhận: Paid, Cancelled.");

            payment.Status = newStatus;
            payment.Notes = dto.Notes;

            if (newStatus == "Paid")
            {
                payment.PaidAt = DateTime.UtcNow;
                
                // Kiểm tra xem tất cả payments của order đã được thanh toán chưa
                if (payment.Order != null)
                {
                    var allPayments = payment.Order.Payments?.ToList() ?? new List<Payment>();
                    var allPaid = allPayments.All(p => p.Status == "Paid");
                    
                    if (allPaid)
                    {
                        payment.Order.PaymentStatus = "Paid";
                    }
                    else
                    {
                        // Có ít nhất một payment đã được thanh toán
                        payment.Order.PaymentStatus = "PartiallyPaid";
                    }
                }
            }
            else if (newStatus == "Cancelled")
            {
                payment.PaidAt = null;
                if (payment.Order != null)
                {
                    // Kiểm tra xem còn payment nào pending/awaiting không
                    var allPayments = payment.Order.Payments?.ToList() ?? new List<Payment>();
                    var hasPendingPayments = allPayments
                        .Any(p => p.Id != payment.Id && (p.Status == "Pending" || p.Status == "AwaitingTransfer"));
                    
                    if (hasPendingPayments)
                    {
                        payment.Order.PaymentStatus = "Pending";
                    }
                    else
                    {
                        // Tất cả payments đều bị hủy
                        payment.Order.PaymentStatus = "Cancelled";
                    }
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        private async Task<Payment> CreatePaymentForEnterpriseAsync(Order order, Enterprise enterprise, decimal amount, string method)
        {
            var reference = GenerateReference(order.Id, enterprise.Id, method);
            string? qrUrl = null;
            string? bankCode = null;
            string? bankAccount = null;
            string? accountName = null;

            if (method == "BankTransfer")
            {
                // Ưu tiên 1: Sử dụng EnterpriseBankInfo (mới)
                var bankInfo = await _context.EnterpriseBankInfos
                    .FirstOrDefaultAsync(ebi => ebi.EnterpriseId == enterprise.Id);

                if (bankInfo != null)
                {
                    bankCode = bankInfo.BankCode;
                    bankAccount = bankInfo.BankAccount;
                    accountName = bankInfo.AccountName;
                }
                // Ưu tiên 2: Sử dụng thông tin từ Enterprise (cũ - tương thích)
                else if (!string.IsNullOrWhiteSpace(enterprise.BankCode) &&
                    !string.IsNullOrWhiteSpace(enterprise.BankAccount) &&
                    !string.IsNullOrWhiteSpace(enterprise.BankAccountName))
                {
                    bankCode = enterprise.BankCode;
                    bankAccount = enterprise.BankAccount;
                    accountName = enterprise.BankAccountName;
                }
                // Ưu tiên 3: Dùng global settings
                else
                {
                    var settings = _bankOptions.Value;
                    if (string.IsNullOrWhiteSpace(settings.BankCode) ||
                        string.IsNullOrWhiteSpace(settings.AccountNumber) ||
                        string.IsNullOrWhiteSpace(settings.AccountName))
                    {
                        throw new InvalidOperationException($"Cấu hình BankTransfer cho Enterprise {enterprise.Name} (ID: {enterprise.Id}) chưa được thiết lập đầy đủ.");
                    }

                    bankCode = settings.BankCode;
                    bankAccount = settings.AccountNumber;
                    accountName = settings.AccountName;
                }

                // Tạo QR code URL với thông tin của enterprise (tương thích với code cũ)
                var enterpriseSettings = new BankTransferSettings
                {
                    BankCode = bankCode,
                    AccountNumber = bankAccount,
                    AccountName = accountName,
                    Template = _bankOptions.Value.Template,
                    BaseUrl = _bankOptions.Value.BaseUrl,
                    Description = $"Thanh toan don hang OCOP - {enterprise.Name}"
                };

                qrUrl = BuildVietQrUrl(amount, reference, enterpriseSettings);
            }
            // COD - không cần QR code, không cần cập nhật PaymentStatus ở đây
            // PaymentStatus sẽ được cập nhật sau khi tạo tất cả payments

            order.PaymentMethod = method;
            if (string.IsNullOrWhiteSpace(order.PaymentReference))
            {
                order.PaymentReference = reference;
            }

            var payment = new Payment
            {
                OrderId = order.Id,
                EnterpriseId = enterprise.Id,
                Amount = amount,
                Method = method,
                Status = method == "BankTransfer" ? "AwaitingTransfer" : "Pending",
                Reference = reference,
                BankCode = bankCode,
                BankAccount = bankAccount,
                AccountName = accountName,
                QrCodeUrl = qrUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            return payment;
        }

        private bool CanAccessPayment(Payment payment, int userId)
        {
            if (User.IsInRole("SystemAdmin"))
                return true;

            if (payment.Order == null)
            {
                payment.Order = _context.Orders.FirstOrDefault(o => o.Id == payment.OrderId);
            }

            if (payment.Order == null)
                return false;

            if (User.IsInRole("Customer"))
                return payment.Order.UserId == userId;

            if (User.IsInRole("EnterpriseAdmin"))
            {
                var enterpriseId = _context.Users.Find(userId)?.EnterpriseId;
                if (enterpriseId == null || enterpriseId == 0) return false;
                return _context.OrderItems
                    .Any(oi => oi.OrderId == payment.OrderId && oi.Product.EnterpriseId == enterpriseId);
            }

            return false;
        }

        private bool CanAccessOrder(Order order, int userId)
        {
            if (User.IsInRole("SystemAdmin"))
                return true;

            if (User.IsInRole("Customer"))
                return order.UserId == userId;

            if (User.IsInRole("EnterpriseAdmin"))
            {
                var enterpriseId = _context.Users.Find(userId)?.EnterpriseId;
                if (enterpriseId == null || enterpriseId == 0) return false;
                return _context.OrderItems
                    .Any(oi => oi.OrderId == order.Id && oi.Product.EnterpriseId == enterpriseId);
            }

            return false;
        }

        private string NormalizeMethod(string? method)
        {
            if (string.IsNullOrWhiteSpace(method))
                return "COD";

            return method.Trim().Equals("BankTransfer", StringComparison.OrdinalIgnoreCase)
                ? "BankTransfer"
                : "COD";
        }

        private string NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "Paid";

            return status.Trim().Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
                ? "Cancelled"
                : "Paid";
        }

        private string GenerateReference(int orderId, int enterpriseId, string method)
        {
            var prefix = method == "BankTransfer" ? "BT" : "COD";
            return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{orderId}-E{enterpriseId}";
        }

        private string BuildVietQrUrl(decimal amount, string reference, BankTransferSettings settings)
        {
            var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
                ? "https://img.vietqr.io/image"
                : settings.BaseUrl.TrimEnd('/');

            var template = string.IsNullOrWhiteSpace(settings.Template) ? "compact" : settings.Template;
            var addInfo = Uri.EscapeDataString(reference);
            var accountName = Uri.EscapeDataString(settings.AccountName);
            var description = Uri.EscapeDataString(settings.Description ?? reference);
            var amountString = amount > 0 ? $"&amount={(int)amount}" : string.Empty;

            return $"{baseUrl}/{settings.BankCode}-{settings.AccountNumber}-{template}.png?addInfo={addInfo}{amountString}&accountName={accountName}&description={description}";
        }

        private PaymentDto MapPaymentToDto(Payment payment)
        {
            return new PaymentDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                EnterpriseId = payment.EnterpriseId,
                EnterpriseName = payment.Enterprise?.Name,
                Amount = payment.Amount,
                Method = payment.Method,
                Status = payment.Status,
                Reference = payment.Reference,
                BankCode = payment.BankCode,
                BankAccount = payment.BankAccount,
                AccountName = payment.AccountName,
                QrCodeUrl = payment.QrCodeUrl,
                Notes = payment.Notes,
                CreatedAt = payment.CreatedAt,
                PaidAt = payment.PaidAt
            };
        }
    }
}

