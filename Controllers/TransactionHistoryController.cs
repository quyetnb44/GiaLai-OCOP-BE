using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionHistoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TransactionHistoryController> _logger;

        public TransactionHistoryController(AppDbContext context, ILogger<TransactionHistoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách lịch sử giao dịch của người dùng hiện tại
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<object>> GetTransactionHistory([FromQuery] TransactionFilterDto filter)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // Query Orders của user
            IQueryable<Order> ordersQuery;
            
            if (role == "Customer")
            {
                ordersQuery = _context.Orders.Where(o => o.UserId == userId.Value);
            }
            else if (role == "EnterpriseAdmin")
            {
                var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
                if (enterpriseId == 0)
                    return Forbid("EnterpriseAdmin không thuộc Enterprise nào.");
                
                ordersQuery = _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId));
            }
            else if (role == "SystemAdmin")
            {
                ordersQuery = _context.Orders;
            }
            else
            {
                return Forbid();
            }

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.Trim().ToLower();
                ordersQuery = ordersQuery.Where(o => 
                    o.Id.ToString().Contains(searchTerm) ||
                    (o.PaymentReference != null && o.PaymentReference.ToLower().Contains(searchTerm))
                );
            }

            if (filter.StartDate.HasValue)
            {
                var startDateUtc = DateTime.SpecifyKind(filter.StartDate.Value, DateTimeKind.Utc);
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= startDateUtc);
            }

            if (filter.EndDate.HasValue)
            {
                var endDateUtc = DateTime.SpecifyKind(filter.EndDate.Value.AddDays(1), DateTimeKind.Utc);
                ordersQuery = ordersQuery.Where(o => o.OrderDate < endDateUtc);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == filter.Status);
            }

            if (!string.IsNullOrWhiteSpace(filter.PaymentMethod))
            {
                ordersQuery = ordersQuery.Where(o => o.PaymentMethod == filter.PaymentMethod);
            }

            // Sorting
            ordersQuery = filter.SortBy?.ToLower() switch
            {
                "date_asc" => ordersQuery.OrderBy(o => o.OrderDate),
                "amount_desc" => ordersQuery.OrderByDescending(o => o.TotalAmount),
                "amount_asc" => ordersQuery.OrderBy(o => o.TotalAmount),
                _ => ordersQuery.OrderByDescending(o => o.OrderDate) // Default: date_desc
            };

            // Pagination
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 20 : (filter.PageSize > 100 ? 100 : filter.PageSize);

            var totalItems = await ordersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var orders = await ordersQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var transactions = orders.Select(order => new TransactionHistoryDto
            {
                TransactionCode = $"ORD-{order.Id}",
                OrderCode = $"ORD-{order.Id}",
                TransactionDate = order.OrderDate,
                Amount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status,
                Type = "order",
                Description = $"Đơn hàng #{order.Id}"
            }).ToList();

            return Ok(new
            {
                items = transactions,
                page,
                pageSize,
                totalItems,
                totalPages
            });
        }

        /// <summary>
        /// Lấy chi tiết một giao dịch/đơn hàng
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TransactionDetailDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<TransactionDetailDto>> GetTransactionDetail(int id)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var order = await _context.Orders
                .Include(o => o.User)
                    .ThenInclude(u => u.Province)
                .Include(o => o.User)
                    .ThenInclude(u => u.District)
                .Include(o => o.User)
                    .ThenInclude(u => u.Ward)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .Include(o => o.Payments)
                    .ThenInclude(p => p.Enterprise)
                .Include(o => o.ShippingAddressDetail)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound("Không tìm thấy giao dịch.");

            // Check authorization
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (role == "Customer")
            {
                if (order.UserId != userId.Value)
                    return Forbid("Bạn chỉ có thể xem giao dịch của chính mình.");
            }
            else if (role == "EnterpriseAdmin")
            {
                var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
                if (enterpriseId == 0)
                    return Forbid("EnterpriseAdmin không thuộc Enterprise nào.");
                
                if (!order.OrderItems.Any(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId))
                    return Forbid("Bạn chỉ có thể xem giao dịch có sản phẩm của doanh nghiệp mình.");
            }
            // SystemAdmin can view all

            // Build shipping address
            string? shippingAddress = null;
            if (order.ShippingAddressId.HasValue && order.ShippingAddressDetail != null)
            {
                var addr = order.ShippingAddressDetail;
                shippingAddress = $"{addr.FullName}, {addr.PhoneNumber}, {addr.AddressLine}, {addr.Ward}, {addr.District}, {addr.Province}";
            }
            else
            {
                shippingAddress = order.ShippingAddress;
            }

            // Map to DTO
            var transactionDetail = new TransactionDetailDto
            {
                Id = order.Id,
                TransactionCode = $"ORD-{order.Id}",
                TransactionDate = order.OrderDate,
                Status = order.Status,
                Type = "order",
                TotalAmount = order.TotalAmount,
                Customer = order.User != null ? new CustomerInfoDto
                {
                    Id = order.User.Id,
                    Name = order.User.Name,
                    Email = order.User.Email,
                    PhoneNumber = order.User.PhoneNumber,
                    AvatarUrl = order.User.AvatarUrl,
                    Address = BuildCustomerAddress(order.User)
                } : null,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDetailDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "Unknown Product",
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    ProductImage = oi.Product?.Images?.FirstOrDefault()?.Url
                }).ToList(),
                Payments = order.Payments.Select(p => new PaymentInfoDto
                {
                    Method = p.Method,
                    Status = p.Status,
                    Reference = p.Reference,
                    MaskedBankAccount = MaskBankAccount(p.BankAccount),
                    BankName = p.Enterprise?.Name,
                    PaidAt = p.PaidAt
                }).ToList(),
                ShippingInfo = new ShippingInfoDto
                {
                    ShipperName = order.ShipperId.HasValue ? $"Shipper #{order.ShipperId}" : null,
                    TrackingNumber = order.PaymentReference,
                    Status = order.Status,
                    ShippedAt = order.ShippedAt,
                    DeliveredAt = order.DeliveredAt,
                    DeliveryNotes = order.DeliveryNotes,
                    ShippingAddress = shippingAddress
                }
            };

            return Ok(transactionDetail);
        }

        #region Helper Methods

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

        private string BuildCustomerAddress(User user)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(user.AddressDetail))
                parts.Add(user.AddressDetail);

            if (user.Ward != null && !string.IsNullOrWhiteSpace(user.Ward.Name))
                parts.Add(user.Ward.Name);

            if (user.District != null && !string.IsNullOrWhiteSpace(user.District.Name))
                parts.Add(user.District.Name);

            if (user.Province != null && !string.IsNullOrWhiteSpace(user.Province.Name))
                parts.Add(user.Province.Name);

            return parts.Count > 0 ? string.Join(", ", parts) : "Chưa cập nhật địa chỉ";
        }

        private string MaskBankAccount(string? account)
        {
            if (string.IsNullOrEmpty(account) || account.Length < 4)
                return "****";
            
            return new string('*', account.Length - 4) + account.Substring(account.Length - 4);
        }

        #endregion
    }
}
