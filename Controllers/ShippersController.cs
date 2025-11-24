using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Shipper,SystemAdmin,EnterpriseAdmin")]
    public class ShippersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ShippersController(AppDbContext context)
        {
            _context = context;
        }

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

        /// <summary>
        /// Lấy danh sách tất cả shippers (EnterpriseAdmin/SystemAdmin)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SystemAdmin,EnterpriseAdmin")]
        public async Task<ActionResult<IEnumerable<ShipperDto>>> GetShippers()
        {
            var shippers = await _context.Users
                .Where(u => u.Role == "Shipper")
                .Select(u => new ShipperDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber
                })
                .OrderBy(u => u.Name)
                .ToListAsync();

            return Ok(shippers);
        }

        /// <summary>
        /// Lấy danh sách đơn hàng cần giao (Shipper chỉ thấy đơn của mình, EnterpriseAdmin/SystemAdmin thấy tất cả)
        /// </summary>
        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersToShip()
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            IQueryable<Order> query = _context.Orders
                .Include(o => o.ShippingAddressDetail) // 🔹 Load ShippingAddressDetail từ database
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                    .ThenInclude(p => p.Enterprise)
                .Where(o => o.Status == "Processing" || o.Status == "Shipped");

            // Shipper chỉ thấy đơn được gán cho mình
            if (role == "Shipper")
            {
                query = query.Where(o => o.ShipperId == userId.Value);
            }
            // EnterpriseAdmin chỉ thấy đơn có sản phẩm của doanh nghiệp mình
            else if (role == "EnterpriseAdmin")
            {
                var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
                if (enterpriseId == 0)
                    return Forbid("EnterpriseAdmin không thuộc Enterprise nào.");

                query = query.Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId));
            }
            // SystemAdmin thấy tất cả

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                OrderDate = o.OrderDate,
                ShippingAddress = o.ShippingAddressId.HasValue && o.ShippingAddressDetail != null
                    ? $"{o.ShippingAddressDetail.FullName}, {o.ShippingAddressDetail.PhoneNumber}, {o.ShippingAddressDetail.AddressLine}, {o.ShippingAddressDetail.Ward}, {o.ShippingAddressDetail.District}, {o.ShippingAddressDetail.Province}"
                    : o.ShippingAddress, // 🔹 Lấy từ database hoặc string
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                PaymentReference = o.PaymentReference,
                ShipperId = o.ShipperId,
                ShippedAt = o.ShippedAt,
                DeliveredAt = o.DeliveredAt,
                DeliveryNotes = o.DeliveryNotes,
                OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList(),
                Payments = o.Payments.Select(p => new PaymentDto
                {
                    Id = p.Id,
                    OrderId = p.OrderId,
                    EnterpriseId = p.EnterpriseId,
                    EnterpriseName = p.Enterprise?.Name,
                    Amount = p.Amount,
                    Method = p.Method,
                    Status = p.Status,
                    Reference = p.Reference,
                    BankCode = p.BankCode,
                    BankAccount = p.BankAccount,
                    AccountName = p.AccountName,
                    QrCodeUrl = p.QrCodeUrl,
                    Notes = p.Notes,
                    CreatedAt = p.CreatedAt,
                    PaidAt = p.PaidAt
                }).ToList()
            }).ToList();

            return Ok(orderDtos);
        }

        /// <summary>
        /// Gán đơn hàng cho Shipper (EnterpriseAdmin/SystemAdmin)
        /// </summary>
        [HttpPost("orders/{orderId}/assign")]
        [Authorize(Roles = "SystemAdmin,EnterpriseAdmin")]
        public async Task<IActionResult> AssignOrderToShipper(int orderId, [FromBody] AssignShipperDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            // Kiểm tra shipper có tồn tại và có role Shipper
            var shipper = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.ShipperId && u.Role == "Shipper");

            if (shipper == null)
                return BadRequest("Shipper không tồn tại hoặc không có quyền giao hàng.");

            // Kiểm tra quyền EnterpriseAdmin
            var userId = await GetUserIdFromTokenAsync();
            if (User.IsInRole("EnterpriseAdmin") && !User.IsInRole("SystemAdmin"))
            {
                var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
                if (enterpriseId == 0)
                    return Forbid("EnterpriseAdmin không thuộc Enterprise nào.");

                var hasAccess = await _context.OrderItems
                    .AnyAsync(oi => oi.OrderId == orderId && oi.Product != null && oi.Product.EnterpriseId == enterpriseId);

                if (!hasAccess)
                    return Forbid("Bạn chỉ có thể gán đơn hàng có sản phẩm của doanh nghiệp mình.");
            }

            if (order.Status != "Processing")
                return BadRequest("Chỉ có thể gán đơn hàng ở trạng thái Processing.");

            order.ShipperId = dto.ShipperId;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã gán đơn hàng cho shipper thành công." });
        }

        /// <summary>
        /// Shipper xác nhận đã nhận đơn hàng và bắt đầu giao (Status: Processing → Shipped)
        /// </summary>
        [HttpPost("orders/{orderId}/ship")]
        [Authorize(Roles = "Shipper")]
        public async Task<IActionResult> ShipOrder(int orderId)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            if (order.ShipperId != userId.Value)
                return Forbid("Bạn chỉ có thể giao đơn hàng được gán cho mình.");

            if (order.Status != "Processing")
                return BadRequest("Chỉ có thể giao đơn hàng ở trạng thái Processing.");

            order.Status = "Shipped";
            order.ShippedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xác nhận bắt đầu giao hàng." });
        }

        /// <summary>
        /// Shipper xác nhận đã giao hàng thành công (Status: Shipped → Completed)
        /// </summary>
        [HttpPost("orders/{orderId}/deliver")]
        [Authorize(Roles = "Shipper")]
        public async Task<IActionResult> DeliverOrder(int orderId, [FromBody] DeliverOrderDto? dto = null)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            if (order.ShipperId != userId.Value)
                return Forbid("Bạn chỉ có thể xác nhận giao hàng cho đơn hàng được gán cho mình.");

            if (order.Status != "Shipped")
                return BadRequest("Chỉ có thể xác nhận giao hàng cho đơn hàng ở trạng thái Shipped.");

            order.Status = "Completed";
            order.DeliveredAt = DateTime.UtcNow;
            order.DeliveryNotes = dto?.Notes;

            // Nếu là COD và chưa thanh toán, tự động cập nhật payment status
            if (order.PaymentMethod == "COD")
            {
                var codPayments = order.Payments.Where(p => p.Method == "COD" && p.Status != "Paid").ToList();
                foreach (var payment in codPayments)
                {
                    payment.Status = "Paid";
                    payment.PaidAt = DateTime.UtcNow;
                    payment.Notes = "Đã thanh toán khi nhận hàng (COD).";
                }

                // Cập nhật Order.PaymentStatus
                if (order.Payments.All(p => p.Status == "Paid"))
                {
                    order.PaymentStatus = "Paid";
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xác nhận giao hàng thành công." });
        }
    }

    public class AssignShipperDto
    {
        public int ShipperId { get; set; }
    }

    public class DeliverOrderDto
    {
        public string? Notes { get; set; }
    }
}

