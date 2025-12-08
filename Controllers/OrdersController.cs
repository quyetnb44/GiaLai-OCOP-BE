using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Options;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // tất cả user phải đăng nhập
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IOptions<BankTransferSettings> _bankOptions;

        public OrdersController(AppDbContext context, IOptions<BankTransferSettings> bankOptions)
        {
            _context = context;
            _bankOptions = bankOptions;
        }

        // Helper method để lấy userId từ token
        private async Task<int?> GetUserIdFromTokenAsync()
        {
            // 🔹 Thử lấy từ ClaimTypes.NameIdentifier trước (userId dạng string)
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(claimValue))
                return null;

            // 🔹 Nếu claim là số (userId), parse trực tiếp
            if (int.TryParse(claimValue, out var userId))
                return userId;

            // 🔹 Nếu claim là email, tìm user từ database
            if (claimValue.Contains("@"))
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == claimValue);
                return user?.Id;
            }

            return null;
        }

        // GET /api/orders
        [HttpGet]
        public async Task<ActionResult<object>> GetOrders(
            [FromQuery] string? status,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            IQueryable<Order> query;

            if (role == "Customer")
            {
                query = _context.Orders
                    .Include(o => o.User) // 🔹 Include User (chính mình)
                        .ThenInclude(u => u.Province) // Include Province để lấy địa chỉ
                    .Include(o => o.User)
                        .ThenInclude(u => u.District) // Include District để lấy địa chỉ
                    .Include(o => o.User)
                        .ThenInclude(u => u.Ward) // Include Ward để lấy địa chỉ
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .Include(o => o.Payments)
                    .Where(o => o.UserId == userId.Value);
            }
            else if (role == "EnterpriseAdmin")
            {
                var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
                if (enterpriseId == 0)
                    return Forbid("EnterpriseAdmin không thuộc Enterprise nào.");
                
                query = _context.Orders
                    .Include(o => o.User) // 🔹 Include User để lấy thông tin Customer
                        .ThenInclude(u => u.Province) // Include Province để lấy địa chỉ
                    .Include(o => o.User)
                        .ThenInclude(u => u.District) // Include District để lấy địa chỉ
                    .Include(o => o.User)
                        .ThenInclude(u => u.Ward) // Include Ward để lấy địa chỉ
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .Include(o => o.Payments)
                    .Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId));
            }
            else if (role == "SystemAdmin")
            {
                query = _context.Orders
                    .Include(o => o.User) // 🔹 Include User để lấy thông tin Customer
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .Include(o => o.Payments);
            }
            else
            {
                return Forbid();
            }

            var filteredQuery = ApplyOrderFilters(query, status, startDate, endDate);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            pageSize = pageSize > 100 ? 100 : pageSize;

            var totalItems = await filteredQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var orders = await filteredQuery
                .Include(o => o.ShippingAddressDetail) // 🔹 Load ShippingAddressDetail từ database
                .Include(o => o.Payments)
                    .ThenInclude(p => p.Enterprise)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            // Map sang DTOs (sau khi đã load dữ liệu từ database)
            var orderDtos = orders.Select(o =>
            {
                // 🔹 Lấy địa chỉ từ ShippingAddressDetail hoặc ShippingAddress (string)
                string? shippingAddress = null;
                if (o.ShippingAddressId.HasValue && o.ShippingAddressDetail != null)
                {
                    // 🔹 Lấy địa chỉ từ ShippingAddressDetail (từ database)
                    var addr = o.ShippingAddressDetail;
                    shippingAddress = $"{addr.FullName}, {addr.PhoneNumber}, {addr.AddressLine}, {addr.Ward}, {addr.District}, {addr.Province}";
                }
                else
                {
                    // 🔹 Lấy địa chỉ từ ShippingAddress (string) - backward compatibility
                    shippingAddress = o.ShippingAddress;
                }

                return new OrderDto
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    OrderDate = o.OrderDate,
                    ShippingAddress = shippingAddress, // 🔹 Lấy từ database hoặc string
                    ShippingAddressId = o.ShippingAddressId, // 🔹 ID địa chỉ từ database
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    PaymentReference = o.PaymentReference,
                    ShipperId = o.ShipperId,
                    ShippedAt = o.ShippedAt,
                    DeliveredAt = o.DeliveredAt,
                    DeliveryNotes = o.DeliveryNotes,
                    // 🔹 Thêm thông tin Customer (để EnterpriseAdmin xem thông tin người đặt hàng)
                    Customer = o.User != null ? new CustomerInfoDto
                    {
                        Id = o.User.Id,
                        Name = o.User.Name,
                        Email = o.User.Email,
                        PhoneNumber = o.User.PhoneNumber,
                        AvatarUrl = o.User.AvatarUrl,
                        Address = BuildCustomerAddress(o.User) // Xây dựng địa chỉ đầy đủ (sau khi đã load từ DB)
                    } : null,
                    OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                    {
                        Id = oi.Id,
                        OrderId = oi.OrderId,
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        Price = oi.Price
                    }).ToList(),
                    Payments = o.Payments.Select(MapPaymentToDto).ToList()
                };
            }).ToList();

            return Ok(new
            {
                items = orderDtos,
                page,
                pageSize,
                totalItems,
                totalPages
            });
        }

        // 🔹 GET /api/orders/{id} - Xem chi tiết 1 đơn hàng
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var order = await _context.Orders
                .Include(o => o.User) // 🔹 Include User để lấy thông tin Customer
                    .ThenInclude(u => u.Province) // Include Province để lấy địa chỉ
                .Include(o => o.User)
                    .ThenInclude(u => u.District) // Include District để lấy địa chỉ
                .Include(o => o.User)
                    .ThenInclude(u => u.Ward) // Include Ward để lấy địa chỉ
                .Include(o => o.ShippingAddressDetail) // 🔹 Load ShippingAddressDetail từ database
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                    .ThenInclude(p => p.Enterprise)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound("Không tìm thấy đơn hàng.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // 🔹 Kiểm tra quyền truy cập
            if (role == "Customer")
            {
                if (order.UserId != userId.Value)
                    return Forbid("Bạn chỉ có thể xem đơn hàng của chính mình.");
            }
            else if (role == "EnterpriseAdmin")
            {
                var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
                if (enterpriseId == 0)
                    return Forbid("EnterpriseAdmin không thuộc Enterprise nào.");
                
                if (!order.OrderItems.Any(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId))
                    return Forbid("Bạn chỉ có thể xem đơn hàng có sản phẩm của doanh nghiệp mình.");
            }
            // SystemAdmin có thể xem tất cả, không cần check

            // 🔹 Lấy địa chỉ từ ShippingAddressDetail hoặc ShippingAddress (string)
            string? shippingAddress = null;
            if (order.ShippingAddressId.HasValue && order.ShippingAddressDetail != null)
            {
                // 🔹 Lấy địa chỉ từ ShippingAddressDetail (từ database)
                var addr = order.ShippingAddressDetail;
                shippingAddress = $"{addr.FullName}, {addr.PhoneNumber}, {addr.AddressLine}, {addr.Ward}, {addr.District}, {addr.Province}";
            }
            else
            {
                // 🔹 Lấy địa chỉ từ ShippingAddress (string) - backward compatibility
                shippingAddress = order.ShippingAddress;
            }

            // 🔹 Load User (Customer) nếu chưa có
            if (order.User == null)
            {
                await _context.Entry(order).Reference(o => o.User).LoadAsync();
            }

            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                ShippingAddress = shippingAddress, // 🔹 Lấy từ database hoặc string
                ShippingAddressId = order.ShippingAddressId, // 🔹 ID địa chỉ từ database
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                PaymentReference = order.PaymentReference,
                ShipperId = order.ShipperId,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                DeliveryNotes = order.DeliveryNotes,
                // 🔹 Thêm thông tin Customer (để EnterpriseAdmin xem thông tin người đặt hàng)
                Customer = order.User != null ? new CustomerInfoDto
                {
                    Id = order.User.Id,
                    Name = order.User.Name,
                    Email = order.User.Email,
                    PhoneNumber = order.User.PhoneNumber,
                    AvatarUrl = order.User.AvatarUrl,
                    Address = BuildCustomerAddress(order.User) // Xây dựng địa chỉ đầy đủ
                } : null,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList(),
                Payments = order.Payments.Select(MapPaymentToDto).ToList()
            };

            return Ok(orderDto);
        }

        // POST /api/orders - Tạo đơn hàng mới (chỉ Customer)
        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            // 🔹 Validation: ShippingAddress hoặc ShippingAddressId
            if (!dto.ShippingAddressId.HasValue && string.IsNullOrEmpty(dto.ShippingAddress))
                return BadRequest("Địa chỉ giao hàng là bắt buộc. Vui lòng cung cấp ShippingAddressId hoặc ShippingAddress.");

            // 🔹 Nếu có ShippingAddressId, kiểm tra xem địa chỉ có tồn tại và thuộc về user hiện tại không
            if (dto.ShippingAddressId.HasValue)
            {
                var shippingAddress = await _context.ShippingAddresses
                    .FirstOrDefaultAsync(sa => sa.Id == dto.ShippingAddressId.Value && sa.UserId == userId.Value);
                
                if (shippingAddress == null)
                    return BadRequest("Địa chỉ giao hàng không tồn tại hoặc không thuộc về bạn.");
            }

            // 🔹 Validation: Items không rỗng
            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest("Đơn hàng phải có ít nhất 1 sản phẩm.");

            // 🔹 Validation: Quantity > 0 cho mỗi item
            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                    return BadRequest($"Số lượng sản phẩm ID {item.ProductId} phải lớn hơn 0.");
            }

            var paymentMethod = NormalizePaymentMethod(dto.PaymentMethod);

            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var orderItemsToCreate = new List<OrderItem>();
            decimal total = 0;

            foreach (var item in dto.Items)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                {
                    return BadRequest($"Sản phẩm ID {item.ProductId} không tồn tại.");
                }

                if (product.Status != "Approved")
                    return BadRequest($"Sản phẩm '{product.Name}' (ID: {item.ProductId}) chưa được duyệt.");

                if (product.StockStatus == "OutOfStock")
                    return BadRequest($"Sản phẩm '{product.Name}' (ID: {item.ProductId}) đã hết hàng.");

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    Price = product.Price
                };

                orderItemsToCreate.Add(orderItem);
                total += product.Price * item.Quantity;
            }

            var order = new Order
            {
                UserId = userId.Value,
                ShippingAddressId = dto.ShippingAddressId, // 🔹 Lưu ShippingAddressId nếu có
                ShippingAddress = dto.ShippingAddressId.HasValue ? null : dto.ShippingAddress, // 🔹 Chỉ lưu string nếu không có ShippingAddressId
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                PaymentMethod = paymentMethod,
                PaymentStatus = "Pending" // Sẽ được cập nhật khi tạo Payment thực sự
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var orderItem in orderItemsToCreate)
                {
                    orderItem.OrderId = order.Id;
                    _context.OrderItems.Add(orderItem);
                }

                order.TotalAmount = total;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // 🔹 Reload để lấy OrderItems và Payments đã lưu
            await _context.Entry(order).Collection(o => o.OrderItems).LoadAsync();
            await _context.Entry(order).Collection(o => o.Payments).LoadAsync();
            
            // Load Enterprise cho mỗi payment
            foreach (var payment in order.Payments)
            {
                await _context.Entry(payment).Reference(p => p.Enterprise).LoadAsync();
            }

            // Map sang DTO
            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                ShippingAddress = order.ShippingAddress,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                PaymentReference = order.PaymentReference,
                ShipperId = order.ShipperId,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                DeliveryNotes = order.DeliveryNotes,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList(),
                Payments = order.Payments.Select(MapPaymentToDto).ToList()
            };

            await CreateOrderNotificationsAsync(order.Id);

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, orderDto);
        }

        private static string NormalizePaymentMethod(string? paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
                return "COD";

            return paymentMethod.Trim().Equals("BankTransfer", StringComparison.OrdinalIgnoreCase)
                ? "BankTransfer"
                : "COD";
        }

        // 🔹 PUT /api/orders/{id}/status - Cập nhật trạng thái đơn hàng
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            // 🔹 Validation: Status hợp lệ
            var validStatuses = new[] { "Pending", "Processing", "Shipped", "Completed", "Cancelled" };
            if (!validStatuses.Contains(dto.Status))
                return BadRequest($"Status không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null) 
                return NotFound("Không tìm thấy đơn hàng.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // 🔹 Phân quyền: Customer chỉ có thể hủy đơn của mình khi EnterpriseAdmin chưa xử lý
            if (role == "Customer")
            {
                if (order.UserId != userId.Value)
                    return Forbid("Bạn chỉ có thể cập nhật đơn hàng của chính mình.");

                if (dto.Status != "Cancelled")
                    return Forbid("Customer chỉ có thể hủy đơn hàng (Cancelled).");

                // Customer chỉ có thể hủy khi đơn hàng vẫn còn ở trạng thái Pending
                // (EnterpriseAdmin chưa xử lý)
                if (order.Status != "Pending")
                    return Forbid("Không thể hủy đơn hàng. Đơn hàng đã được doanh nghiệp xử lý (trạng thái: " + order.Status + ").");
            }
            // 🔹 Phân quyền: EnterpriseAdmin chỉ có thể cập nhật đơn hàng có sản phẩm của Enterprise mình
            else if (role == "EnterpriseAdmin")
            {
                var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
                if (enterpriseId == 0)
                    return Forbid("EnterpriseAdmin không thuộc Enterprise nào.");

                var hasAccess = order.OrderItems.Any(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId);
                if (!hasAccess)
                    return Forbid("Bạn chỉ có thể cập nhật đơn hàng có sản phẩm của doanh nghiệp mình.");

                // EnterpriseAdmin không thể set status = "Cancelled" (chỉ Customer mới có thể hủy)
                if (dto.Status == "Cancelled")
                    return Forbid("EnterpriseAdmin không thể hủy đơn hàng. Chỉ Customer mới có thể hủy đơn hàng.");
            }
            // SystemAdmin có thể cập nhật bất kỳ status nào

            order.Status = dto.Status;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 🔹 PUT /api/orders/{id}/shipping-address - Cập nhật địa chỉ giao hàng
        [HttpPut("{id}/shipping-address")]
        public async Task<IActionResult> UpdateShippingAddress(int id, [FromBody] UpdateShippingAddressDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // Chỉ Customer mới được cập nhật địa chỉ giao hàng của đơn hàng của mình
            if (role != "Customer" || order.UserId != userId.Value)
                return Forbid("Bạn chỉ có thể cập nhật địa chỉ giao hàng của đơn hàng của chính mình.");

            // Chỉ cho phép cập nhật khi đơn hàng còn ở trạng thái Pending hoặc Processing
            if (order.Status != "Pending" && order.Status != "Processing")
                return BadRequest("Chỉ có thể cập nhật địa chỉ giao hàng khi đơn hàng ở trạng thái Pending hoặc Processing.");

            order.ShippingAddress = dto.ShippingAddress.Trim();
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 🔹 DELETE /api/orders/{id} - Xóa đơn hàng
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound("Không tìm thấy đơn hàng.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // 🔹 Phân quyền
            if (role == "Customer")
            {
                if (order.UserId != userId.Value)
                    return Forbid("Bạn chỉ có thể xóa đơn hàng của chính mình.");

                // Customer có thể xóa đơn ở trạng thái Pending hoặc Cancelled
                if (order.Status != "Pending" && order.Status != "Cancelled")
                    return BadRequest("Chỉ có thể xóa đơn hàng ở trạng thái Pending hoặc Cancelled.");
            }
            else if (role == "EnterpriseAdmin")
            {
                var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
                if (enterpriseId == 0)
                    return Forbid("EnterpriseAdmin không thuộc Enterprise nào.");
                
                if (!order.OrderItems.Any(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId))
                    return Forbid("Bạn chỉ có thể xóa đơn hàng có sản phẩm của doanh nghiệp mình.");
                
                // EnterpriseAdmin chỉ có thể xóa đơn ở trạng thái Pending hoặc Cancelled
                if (order.Status != "Pending" && order.Status != "Cancelled")
                    return BadRequest("Chỉ có thể xóa đơn hàng ở trạng thái Pending hoặc Cancelled.");
            }
            // SystemAdmin có thể xóa bất kỳ đơn hàng nào

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private IQueryable<Order> ApplyOrderFilters(IQueryable<Order> query, string? status, DateTime? startDate, DateTime? endDate)
        {
            if (!string.IsNullOrWhiteSpace(status))
            {
                var statuses = status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (statuses.Count > 0)
                {
                    var normalizedStatuses = statuses
                        .Select(s => s.ToUpperInvariant())
                        .ToList();

                    query = query.Where(o => normalizedStatuses.Contains(o.Status.ToUpper()));
                }
            }

            if (startDate.HasValue)
            {
                var from = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                query = query.Where(o => o.OrderDate >= from);
            }

            if (endDate.HasValue)
            {
                var to = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
                query = query.Where(o => o.OrderDate <= to);
            }

            return query;
        }

        private BankTransferSettings GetSystemAdminBankSettings()
        {
            var settings = _bankOptions.Value;

            if (string.IsNullOrWhiteSpace(settings.BankCode) ||
                string.IsNullOrWhiteSpace(settings.AccountNumber) ||
                string.IsNullOrWhiteSpace(settings.AccountName))
            {
                throw new InvalidOperationException("Thông tin ngân hàng của SystemAdmin chưa được cấu hình đầy đủ.");
            }

            return settings;
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

        private async Task CreateOrderNotificationsAsync(int orderId)
        {
            var enterpriseIds = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId && oi.Product != null)
                .Select(oi => oi.Product!.EnterpriseId)
                .Distinct()
                .ToListAsync();

            if (!enterpriseIds.Any())
                return;

            foreach (var enterpriseId in enterpriseIds)
            {
                _context.Notifications.Add(new Notification
                {
                    Type = "new_order",
                    Title = $"Đơn hàng mới #{orderId}",
                    Message = "Bạn có đơn hàng mới cần xử lý.",
                    EnterpriseId = enterpriseId,
                    OrderId = orderId,
                    Link = $"/orders/{orderId}"
                });
            }

            await _context.SaveChangesAsync();
        }

        private PaymentDto MapPaymentToDto(Payment payment)
        {
            var normalizedReference = string.IsNullOrWhiteSpace(payment.Reference)
                ? $"BT-{payment.OrderId}-E{payment.EnterpriseId}"
                : payment.Reference;

            var dto = new PaymentDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                EnterpriseId = payment.EnterpriseId,
                EnterpriseName = payment.Enterprise?.Name,
                Amount = payment.Amount,
                Method = payment.Method,
                Status = payment.Status,
                Reference = normalizedReference,
                BankCode = payment.BankCode,
                BankAccount = payment.BankAccount,
                AccountName = payment.AccountName,
                QrCodeUrl = payment.QrCodeUrl,
                Notes = payment.Notes,
                CreatedAt = payment.CreatedAt,
                PaidAt = payment.PaidAt
            };

            if (payment.Method == "BankTransfer")
            {
                var systemBankSettings = GetSystemAdminBankSettings();
                var settings = new BankTransferSettings
                {
                    BankCode = systemBankSettings.BankCode,
                    AccountNumber = systemBankSettings.AccountNumber,
                    AccountName = systemBankSettings.AccountName,
                    Template = _bankOptions.Value.Template,
                    BaseUrl = _bankOptions.Value.BaseUrl,
                    Description = $"Thanh toan don hang #{payment.OrderId}"
                };

                dto.BankCode = settings.BankCode;
                dto.BankAccount = settings.AccountNumber;
                dto.AccountName = settings.AccountName;
                dto.QrCodeUrl = BuildVietQrUrl(payment.Amount, normalizedReference, settings);
            }

            return dto;
        }

        // Helper: Xây dựng địa chỉ đầy đủ của Customer
        private string? BuildCustomerAddress(User? user)
        {
            if (user == null) return null;

            var addressParts = new List<string>();

            // Thêm địa chỉ chi tiết (số nhà, đường)
            if (!string.IsNullOrWhiteSpace(user.AddressDetail))
            {
                addressParts.Add(user.AddressDetail);
            }

            // Thêm Phường/Xã (nếu đã được Include)
            if (user.Ward != null && !string.IsNullOrWhiteSpace(user.Ward.Name))
            {
                addressParts.Add(user.Ward.Name);
            }

            // Thêm Quận/Huyện (nếu đã được Include)
            if (user.District != null && !string.IsNullOrWhiteSpace(user.District.Name))
            {
                addressParts.Add(user.District.Name);
            }

            // Thêm Tỉnh/Thành phố (nếu đã được Include)
            if (user.Province != null && !string.IsNullOrWhiteSpace(user.Province.Name))
            {
                addressParts.Add(user.Province.Name);
            }

            // Nếu có địa chỉ từ các phần trên, trả về
            if (addressParts.Count > 0)
            {
                return string.Join(", ", addressParts);
            }

            // Fallback: Trả về ShippingAddress cũ nếu có
            return user.ShippingAddress;
        }
    }

    // 🔹 DTO cho Update Order Status
    public class UpdateOrderStatusDto
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Status là bắt buộc.")]
        [System.ComponentModel.DataAnnotations.RegularExpression("^(Pending|Processing|Shipped|Completed|Cancelled)$", 
            ErrorMessage = "Status không hợp lệ. Chỉ chấp nhận: Pending, Processing, Shipped, Completed, Cancelled")]
        public string Status { get; set; } = string.Empty;
    }
}