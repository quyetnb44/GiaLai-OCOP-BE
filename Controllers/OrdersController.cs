using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Options;
using GiaLaiOCOP.Api.Services;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // tất cả user phải đăng nhập
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IOptions<BankTransferSettings> _bankOptions;
        private readonly IWalletService _walletService;

        public OrdersController(
            AppDbContext context, 
            IOptions<BankTransferSettings> bankOptions,
            IWalletService walletService)
        {
            _context = context;
            _bankOptions = bankOptions;
            _walletService = walletService;
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
                        .ThenInclude(p => p.Enterprise) // 🔹 Include Enterprise để lấy EnterpriseName và ImageUrl
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
                        .ThenInclude(p => p.Enterprise) // 🔹 Include Enterprise để lấy EnterpriseName
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
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Enterprise)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 🔹 Load OrderEnterpriseStatus cho tất cả orders
            var orderIds = orders.Select(o => o.Id).ToList();
            var enterpriseStatuses = await _context.OrderEnterpriseStatuses
                .Include(oes => oes.Enterprise)
                .Where(oes => orderIds.Contains(oes.OrderId))
                .ToListAsync();
            
            // 🔹 Lấy enterpriseId cho EnterpriseAdmin (để filter orderItems)
            int? currentEnterpriseId = null;
            if (role == "EnterpriseAdmin")
            {
                var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
                if (enterpriseId > 0)
                {
                    currentEnterpriseId = enterpriseId;
                }
            }

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

                // 🔹 Filter orderItems: Nếu là EnterpriseAdmin, chỉ trả về orderItems thuộc enterprise của họ
                var orderItemsToReturn = o.OrderItems.AsEnumerable();
                if (currentEnterpriseId.HasValue)
                {
                    // Chỉ lấy orderItems có Product thuộc enterprise này
                    orderItemsToReturn = o.OrderItems.Where(oi => oi.Product != null && oi.Product.EnterpriseId == currentEnterpriseId.Value);
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
                    BankTransferRejectionReason = o.BankTransferRejectionReason,
                    ShipperId = o.ShipperId,
                    ShippedAt = o.ShippedAt,
                    DeliveredAt = o.DeliveredAt,
                    DeliveryNotes = o.DeliveryNotes,
                    CompletionRequestedAt = o.CompletionRequestedAt,
                    CompletionApprovedAt = o.CompletionApprovedAt,
                    CompletionRejectedAt = o.CompletionRejectedAt,
                    CompletionRejectionReason = o.CompletionRejectionReason,
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
                    OrderItems = orderItemsToReturn.Select(oi => new OrderItemDto
                    {
                        Id = oi.Id,
                        OrderId = oi.OrderId,
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        EnterpriseId = oi.Product?.EnterpriseId,
                        EnterpriseName = oi.Product?.Enterprise?.Name,
                        EnterpriseImageUrl = oi.Product?.Enterprise?.ImageUrl,
                        ProductName = oi.Product?.Name,
                        ProductImageUrl = oi.Product?.ImageUrl
                    }).ToList(),
                    Payments = o.Payments.Select(MapPaymentToDto).ToList(),
                    // 🔹 Thêm trạng thái riêng của từng Enterprise (chỉ cho SystemAdmin)
                    EnterpriseStatuses = role == "SystemAdmin" 
                        ? enterpriseStatuses
                            .Where(oes => oes.OrderId == o.Id)
                            .Select(oes => new OrderEnterpriseStatusDto
                            {
                                Id = oes.Id,
                                OrderId = oes.OrderId,
                                EnterpriseId = oes.EnterpriseId,
                                EnterpriseName = oes.Enterprise?.Name,
                                Status = oes.Status,
                                UpdatedAt = oes.UpdatedAt,
                                UpdatedBy = oes.UpdatedBy,
                                Notes = oes.Notes
                            }).ToList()
                        : null
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
                    .ThenInclude(p => p.Enterprise) // 🔹 Include Enterprise để lấy EnterpriseName và ImageUrl
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

            // 🔹 Filter orderItems: Nếu là EnterpriseAdmin, chỉ trả về orderItems thuộc enterprise của họ
            var orderItemsToReturn = order.OrderItems.AsEnumerable();
            if (role == "EnterpriseAdmin")
            {
                var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
                if (enterpriseId > 0)
                {
                    // Chỉ lấy orderItems có Product thuộc enterprise này
                    orderItemsToReturn = order.OrderItems.Where(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId);
                }
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
                BankTransferRejectionReason = order.BankTransferRejectionReason,
                ShipperId = order.ShipperId,
                    ShippedAt = order.ShippedAt,
                    DeliveredAt = order.DeliveredAt,
                    DeliveryNotes = order.DeliveryNotes,
                    CompletionRequestedAt = order.CompletionRequestedAt,
                    CompletionApprovedAt = order.CompletionApprovedAt,
                    CompletionRejectedAt = order.CompletionRejectedAt,
                    CompletionRejectionReason = order.CompletionRejectionReason,
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
                OrderItems = orderItemsToReturn.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    EnterpriseId = oi.Product?.EnterpriseId,
                    EnterpriseName = oi.Product?.Enterprise?.Name,
                    EnterpriseImageUrl = oi.Product?.Enterprise?.ImageUrl,
                    ProductName = oi.Product?.Name,
                    ProductImageUrl = oi.Product?.ImageUrl
                }).ToList(),
                Payments = order.Payments.Select(MapPaymentToDto).ToList(),
                // 🔹 Thêm trạng thái riêng của từng Enterprise (chỉ cho SystemAdmin)
                EnterpriseStatuses = role == "SystemAdmin"
                    ? (await _context.OrderEnterpriseStatuses
                        .Include(oes => oes.Enterprise)
                        .Where(oes => oes.OrderId == order.Id)
                        .Select(oes => new OrderEnterpriseStatusDto
                        {
                            Id = oes.Id,
                            OrderId = oes.OrderId,
                            EnterpriseId = oes.EnterpriseId,
                            EnterpriseName = oes.Enterprise != null ? oes.Enterprise.Name : null,
                            Status = oes.Status,
                            UpdatedAt = oes.UpdatedAt,
                            UpdatedBy = oes.UpdatedBy,
                            Notes = oes.Notes
                        }).ToListAsync())
                    : null
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
                PaymentStatus = paymentMethod == "BankTransfer" ? "AwaitingTransfer" : "Pending" // BankTransfer cần xét duyệt, COD là Pending
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

                // 🔹 Tạo OrderEnterpriseStatus cho mỗi enterprise có sản phẩm trong đơn hàng
                var enterpriseIds = orderItemsToCreate
                    .Select(oi => products[oi.ProductId].EnterpriseId)
                    .Distinct()
                    .ToList();

                foreach (var enterpriseId in enterpriseIds)
                {
                    var enterpriseStatus = new OrderEnterpriseStatus
                    {
                        OrderId = order.Id,
                        EnterpriseId = enterpriseId,
                        Status = "Pending",
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.OrderEnterpriseStatuses.Add(enterpriseStatus);
                }

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
            
            // Load Product và Enterprise cho mỗi OrderItem
            foreach (var item in order.OrderItems)
            {
                await _context.Entry(item).Reference(oi => oi.Product).LoadAsync();
                if (item.Product != null)
                {
                    await _context.Entry(item.Product).Reference(p => p.Enterprise).LoadAsync();
                }
            }
            
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
                BankTransferRejectionReason = order.BankTransferRejectionReason,
                ShipperId = order.ShipperId,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                DeliveryNotes = order.DeliveryNotes,
                CompletionRequestedAt = order.CompletionRequestedAt,
                CompletionApprovedAt = order.CompletionApprovedAt,
                CompletionRejectedAt = order.CompletionRejectedAt,
                CompletionRejectionReason = order.CompletionRejectionReason,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    EnterpriseId = oi.Product?.EnterpriseId,
                    EnterpriseName = oi.Product?.Enterprise?.Name,
                    EnterpriseImageUrl = oi.Product?.Enterprise?.ImageUrl,
                    ProductName = oi.Product?.Name,
                    ProductImageUrl = oi.Product?.ImageUrl
                }).ToList(),
                Payments = order.Payments.Select(MapPaymentToDto).ToList()
            };

            await CreateOrderNotificationsAsync(order.Id);

            // 🔹 Nếu là BankTransfer, tạo notification cho SystemAdmin để xét duyệt chuyển khoản
            if (paymentMethod == "BankTransfer")
            {
                await CreateBankTransferNotificationAsync(order.Id);
            }

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
            var validStatuses = new[] { "Pending", "Processing", "Shipped", "Completed", "Cancelled", "PendingCompletion" };
            if (!validStatuses.Contains(dto.Status))
                return BadRequest($"Status không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null) 
                return NotFound("Không tìm thấy đơn hàng.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // 🔹 Phân quyền: Customer chỉ có thể hủy đơn của mình khi đơn hàng còn ở trạng thái Pending
            if (role == "Customer")
            {
                if (order.UserId != userId.Value)
                    return Forbid("Bạn chỉ có thể cập nhật đơn hàng của chính mình.");

                if (dto.Status != "Cancelled")
                    return Forbid("Customer chỉ có thể hủy đơn hàng (Cancelled).");

                // Customer chỉ có thể hủy khi đơn hàng vẫn còn ở trạng thái Pending
                if (order.Status != "Pending")
                    return Forbid("Không thể hủy đơn hàng. Đơn hàng đã được xử lý (trạng thái: " + order.Status + ").");

                order.Status = dto.Status;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            // 🔹 EnterpriseAdmin chỉ có thể chấp nhận đơn hàng (Pending → Processing)
            else if (role == "EnterpriseAdmin")
            {
                var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
                if (enterpriseId == 0)
                    return Forbid("EnterpriseAdmin không thuộc Enterprise nào.");

                var hasAccess = order.OrderItems.Any(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId);
                if (!hasAccess)
                    return Forbid("Bạn chỉ có thể chấp nhận đơn hàng có sản phẩm của doanh nghiệp mình.");

                // EnterpriseAdmin chỉ có thể chấp nhận đơn hàng (Pending → Processing)
                if (order.Status != "Pending")
                    return BadRequest("Chỉ có thể chấp nhận đơn hàng khi đơn hàng ở trạng thái Pending. Trạng thái hiện tại: " + order.Status);

                if (dto.Status != "Processing")
                    return Forbid("EnterpriseAdmin chỉ có thể chấp nhận đơn hàng (chuyển từ Pending sang Processing). Các trạng thái khác sẽ do SystemAdmin xử lý.");

                // 🔹 Cập nhật trạng thái của enterprise này trong OrderEnterpriseStatus
                var enterpriseStatus = await _context.OrderEnterpriseStatuses
                    .FirstOrDefaultAsync(oes => oes.OrderId == id && oes.EnterpriseId == enterpriseId);

                if (enterpriseStatus == null)
                {
                    // Tạo mới nếu chưa có
                    enterpriseStatus = new OrderEnterpriseStatus
                    {
                        OrderId = id,
                        EnterpriseId = enterpriseId,
                        Status = "Processing",
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = userId.Value
                    };
                    _context.OrderEnterpriseStatuses.Add(enterpriseStatus);
                }
                else
                {
                    // Cập nhật nếu đã có
                    enterpriseStatus.Status = "Processing";
                    enterpriseStatus.UpdatedAt = DateTime.UtcNow;
                    enterpriseStatus.UpdatedBy = userId.Value;
                }

                await _context.SaveChangesAsync();

                // 🔹 Kiểm tra xem tất cả các enterprise có orderItems trong đơn hàng đã chấp nhận chưa
                var allEnterpriseIds = order.OrderItems
                    .Where(oi => oi.Product != null)
                    .Select(oi => oi.Product!.EnterpriseId)
                    .Distinct()
                    .ToList();

                var allEnterpriseStatuses = await _context.OrderEnterpriseStatuses
                    .Where(oes => oes.OrderId == id && allEnterpriseIds.Contains(oes.EnterpriseId))
                    .ToListAsync();

                // Kiểm tra xem tất cả các enterprise đã chấp nhận (Processing) chưa
                var allEnterprisesHaveStatus = allEnterpriseIds.All(eid => 
                    allEnterpriseStatuses.Any(oes => oes.EnterpriseId == eid));

                if (allEnterprisesHaveStatus)
                {
                    // Kiểm tra xem tất cả các enterprise đã chấp nhận (Processing) chưa
                    var allHaveAccepted = allEnterpriseStatuses.All(oes => oes.Status == "Processing");

                    if (allHaveAccepted)
                    {
                        // Tất cả các enterprise đã chấp nhận → Cập nhật trạng thái tổng thể của đơn hàng thành Processing
                        order.Status = "Processing";
                        await _context.SaveChangesAsync();

                        // Tạo notification cho SystemAdmin về việc TẤT CẢ enterprise đã chấp nhận
                        var systemAdmins = await _context.Users
                            .Where(u => u.Role == "SystemAdmin")
                            .ToListAsync();

                        foreach (var admin in systemAdmins)
                        {
                            _context.Notifications.Add(new Notification
                            {
                                Type = "order_accepted",
                                Title = $"Đơn hàng #{order.Id} đã được tất cả EnterpriseAdmin chấp nhận",
                                Message = $"Tất cả EnterpriseAdmin đã chấp nhận đơn hàng #{order.Id}. Bạn có thể gán shipper và cập nhật trạng thái đơn hàng.",
                                UserId = admin.Id,
                                OrderId = order.Id,
                                Link = $"/admin?tab=order-management",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    else
                    {
                        // Có enterprise chưa chấp nhận → Tạo notification cho SystemAdmin về việc enterprise này đã chấp nhận
                        var enterprise = await _context.Enterprises.FindAsync(enterpriseId);
                        var enterpriseName = enterprise?.Name ?? $"Enterprise #{enterpriseId}";
                        
                        var systemAdmins = await _context.Users
                            .Where(u => u.Role == "SystemAdmin")
                            .ToListAsync();

                        foreach (var admin in systemAdmins)
                        {
                            _context.Notifications.Add(new Notification
                            {
                                Type = "order_accepted",
                                Title = $"Đơn hàng #{order.Id} - {enterpriseName} đã chấp nhận",
                                Message = $"{enterpriseName} đã chấp nhận đơn hàng #{order.Id}. Đang chờ các doanh nghiệp khác chấp nhận.",
                                UserId = admin.Id,
                                OrderId = order.Id,
                                Link = $"/admin?tab=order-management",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
                else
                {
                    // Chưa tất cả enterprise đã cập nhật → Tạo notification cho SystemAdmin về việc enterprise này đã chấp nhận
                    var enterprise = await _context.Enterprises.FindAsync(enterpriseId);
                    var enterpriseName = enterprise?.Name ?? $"Enterprise #{enterpriseId}";
                    
                    var systemAdmins = await _context.Users
                        .Where(u => u.Role == "SystemAdmin")
                        .ToListAsync();

                    foreach (var admin in systemAdmins)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            Type = "order_accepted",
                            Title = $"Đơn hàng #{order.Id} - {enterpriseName} đã chấp nhận",
                            Message = $"{enterpriseName} đã chấp nhận đơn hàng #{order.Id}. Đang chờ các doanh nghiệp khác chấp nhận.",
                            UserId = admin.Id,
                            OrderId = order.Id,
                            Link = $"/admin?tab=order-management",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            // 🔹 SystemAdmin có quyền cập nhật bất kỳ status nào (cập nhật trực tiếp trạng thái tổng thể)
            else if (role == "SystemAdmin")
            {
                var oldStatus = order.Status;
                order.Status = dto.Status;
                await _context.SaveChangesAsync();

                // Tạo notification cho EnterpriseAdmin khi SystemAdmin cập nhật trạng thái từ Processing → Shipped
                if (oldStatus == "Processing" && dto.Status == "Shipped")
                {
                    var enterpriseIds = order.OrderItems
                        .Where(oi => oi.Product != null)
                        .Select(oi => oi.Product!.EnterpriseId)
                        .Distinct()
                        .ToList();

                    foreach (var enterpriseId in enterpriseIds)
                    {
                        var enterpriseAdmin = await _context.Users
                            .FirstOrDefaultAsync(u => u.EnterpriseId == enterpriseId && u.Role == "EnterpriseAdmin");

                        if (enterpriseAdmin != null)
                        {
                            _context.Notifications.Add(new Notification
                            {
                                Type = "order_shipped",
                                Title = $"Đơn hàng #{order.Id} đã được giao",
                                Message = $"SystemAdmin đã cập nhật trạng thái đơn hàng #{order.Id} thành \"Đang giao\". Bạn có thể gửi yêu cầu xác nhận hoàn thành khi đã giao xong.",
                                UserId = enterpriseAdmin.Id,
                                EnterpriseId = enterpriseId,
                                OrderId = order.Id,
                                Link = $"/enterprise-admin?tab=orders",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                return NoContent();
            }

            // Customer đã được xử lý ở trên
            return NoContent();
        }

        // 🔹 POST /api/orders/{id}/request-completion - EnterpriseAdmin gửi yêu cầu xác nhận hoàn thành
        [Authorize(Roles = "EnterpriseAdmin")]
        [HttpPost("{id}/request-completion")]
        public async Task<ActionResult<OrderDto>> RequestOrderCompletion(int id, [FromBody] RequestOrderCompletionDto dto)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            // Kiểm tra quyền: EnterpriseAdmin chỉ có thể request completion cho đơn hàng có sản phẩm của Enterprise mình
            var enterpriseId = (await _context.Users.FindAsync(userId.Value))?.EnterpriseId ?? 0;
            if (enterpriseId == 0)
                return Forbid("EnterpriseAdmin không thuộc Enterprise nào.");

            var hasAccess = order.OrderItems.Any(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId);
            if (!hasAccess)
                return Forbid("Bạn chỉ có thể gửi yêu cầu xác nhận hoàn thành cho đơn hàng có sản phẩm của doanh nghiệp mình.");

            // Chỉ cho phép request completion khi đơn hàng ở trạng thái "Shipped"
            if (order.Status != "Shipped")
                return BadRequest("Chỉ có thể gửi yêu cầu xác nhận hoàn thành khi đơn hàng ở trạng thái Shipped.");

            // Cập nhật trạng thái và thông tin completion
            order.Status = "PendingCompletion";
            order.CompletionRequestedAt = DateTime.UtcNow;
            order.CompletionRejectedAt = null; // Reset rejection info nếu có
            order.CompletionRejectionReason = null;

            await _context.SaveChangesAsync();

            // Tạo notification cho SystemAdmin
            await CreateCompletionRequestNotificationAsync(order.Id, enterpriseId);

            // Reload order để trả về đầy đủ thông tin
            await _context.Entry(order).Reference(o => o.User).LoadAsync();
            await _context.Entry(order).Collection(o => o.OrderItems).LoadAsync();
            await _context.Entry(order).Collection(o => o.Payments).LoadAsync();
            
            // Load Product và Enterprise cho OrderItems
            foreach (var item in order.OrderItems)
            {
                await _context.Entry(item).Reference(oi => oi.Product).LoadAsync();
                if (item.Product != null)
                {
                    await _context.Entry(item.Product).Reference(p => p.Enterprise).LoadAsync();
                }
            }

            // Lấy enterpriseId nếu là EnterpriseAdmin (để filter orderItems trong DTO)
            int? entIdForDto = null;
            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (currentRole == "EnterpriseAdmin")
            {
                var currentUserId = await GetUserIdFromTokenAsync();
                if (currentUserId.HasValue)
                {
                    var user = await _context.Users.FindAsync(currentUserId.Value);
                    entIdForDto = user?.EnterpriseId;
                }
            }

            var orderDto = await MapOrderToDtoAsync(order, entIdForDto, currentRole);
            return Ok(orderDto);
        }

        // 🔹 POST /api/orders/{id}/confirm-bank-transfer - SystemAdmin xác nhận hoặc từ chối chuyển khoản ngân hàng
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost("{id}/confirm-bank-transfer")]
        public async Task<ActionResult<OrderDto>> ConfirmBankTransfer(int id, [FromBody] ConfirmBankTransferDto dto)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            // Chỉ có thể xác nhận/chưa chuyển khoản khi paymentMethod là BankTransfer và paymentStatus là AwaitingTransfer hoặc BankTransferRejected
            if (order.PaymentMethod != "BankTransfer")
                return BadRequest("Đơn hàng này không sử dụng phương thức thanh toán chuyển khoản ngân hàng.");

            if (order.PaymentStatus != "AwaitingTransfer" && order.PaymentStatus != "BankTransferRejected")
                return BadRequest($"Chỉ có thể xác nhận chuyển khoản khi đơn hàng ở trạng thái AwaitingTransfer hoặc BankTransferRejected. Trạng thái hiện tại: {order.PaymentStatus}");

            if (dto.Confirmed)
            {
                // Xác nhận đã chuyển khoản: Cập nhật PaymentStatus thành "BankTransferConfirmed"
                order.PaymentStatus = "BankTransferConfirmed";
                order.BankTransferRejectionReason = null; // Xóa lý do từ chối nếu có
                
                // Cập nhật tất cả Payments của đơn hàng này thành "Paid"
                foreach (var payment in order.Payments.Where(p => p.Method == "BankTransfer"))
                {
                    payment.Status = "Paid";
                    payment.PaidAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Tạo notification cho Customer
                _context.Notifications.Add(new Notification
                {
                    Type = "bank_transfer_confirmed",
                    Title = $"Đã xác nhận chuyển khoản cho đơn hàng #{order.Id}",
                    Message = $"SystemAdmin đã xác nhận đã nhận được chuyển khoản cho đơn hàng #{order.Id}. Đơn hàng sẽ được xử lý tiếp theo.",
                    UserId = order.UserId,
                    OrderId = order.Id,
                    Link = $"/orders/{order.Id}",
                    CreatedAt = DateTime.UtcNow
                });

                // Tạo notification cho EnterpriseAdmin của các enterprise có sản phẩm trong đơn hàng
                var enterpriseIds = order.OrderItems
                    .Where(oi => oi.Product != null)
                    .Select(oi => oi.Product!.EnterpriseId)
                    .Distinct()
                    .ToList();

                foreach (var enterpriseId in enterpriseIds)
                {
                    var enterpriseAdmin = await _context.Users
                        .FirstOrDefaultAsync(u => u.EnterpriseId == enterpriseId && u.Role == "EnterpriseAdmin");

                    if (enterpriseAdmin != null)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            Type = "bank_transfer_confirmed",
                            Title = $"Đã xác nhận chuyển khoản cho đơn hàng #{order.Id}",
                            Message = $"SystemAdmin đã xác nhận đã nhận được chuyển khoản cho đơn hàng #{order.Id}. SystemAdmin sẽ tiếp tục xử lý đơn hàng.",
                            UserId = enterpriseAdmin.Id,
                            EnterpriseId = enterpriseId,
                            OrderId = order.Id,
                            Link = $"/enterprise-admin?tab=orders",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            else
            {
                // Chưa chuyển khoản: Cập nhật PaymentStatus thành "BankTransferRejected" và lưu lý do
                if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                    return BadRequest("Vui lòng nhập lý do chưa chuyển khoản.");

                order.PaymentStatus = "BankTransferRejected";
                order.BankTransferRejectionReason = dto.RejectionReason.Trim();
                
                await _context.SaveChangesAsync();

                // Tạo notification cho Customer
                _context.Notifications.Add(new Notification
                {
                    Type = "bank_transfer_rejected",
                    Title = $"Chưa nhận được chuyển khoản cho đơn hàng #{order.Id}",
                    Message = $"SystemAdmin chưa nhận được chuyển khoản cho đơn hàng #{order.Id}. Lý do: {dto.RejectionReason}. Vui lòng kiểm tra lại và thực hiện chuyển khoản.",
                    UserId = order.UserId,
                    OrderId = order.Id,
                    Link = $"/payment/{order.Id}",
                    CreatedAt = DateTime.UtcNow
                });

                // Tạo notification cho EnterpriseAdmin
                var enterpriseIds = order.OrderItems
                    .Where(oi => oi.Product != null)
                    .Select(oi => oi.Product!.EnterpriseId)
                    .Distinct()
                    .ToList();

                foreach (var enterpriseId in enterpriseIds)
                {
                    var enterpriseAdmin = await _context.Users
                        .FirstOrDefaultAsync(u => u.EnterpriseId == enterpriseId && u.Role == "EnterpriseAdmin");

                    if (enterpriseAdmin != null)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            Type = "bank_transfer_rejected",
                            Title = $"Chưa nhận được chuyển khoản cho đơn hàng #{order.Id}",
                            Message = $"SystemAdmin chưa nhận được chuyển khoản cho đơn hàng #{order.Id}. Lý do: {dto.RejectionReason}. Đơn hàng sẽ được xử lý sau khi nhận được chuyển khoản.",
                            UserId = enterpriseAdmin.Id,
                            EnterpriseId = enterpriseId,
                            OrderId = order.Id,
                            Link = $"/enterprise-admin?tab=orders",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Reload order để trả về đầy đủ thông tin
            await _context.Entry(order).Reference(o => o.User).LoadAsync();
            await _context.Entry(order).Collection(o => o.OrderItems).LoadAsync();
            await _context.Entry(order).Collection(o => o.Payments).LoadAsync();
            
            // Load Product và Enterprise cho OrderItems
            foreach (var item in order.OrderItems)
            {
                await _context.Entry(item).Reference(oi => oi.Product).LoadAsync();
                if (item.Product != null)
                {
                    await _context.Entry(item.Product).Reference(p => p.Enterprise).LoadAsync();
                }
            }

            // Load Enterprise cho Payments
            foreach (var payment in order.Payments)
            {
                await _context.Entry(payment).Reference(p => p.Enterprise).LoadAsync();
            }

            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var orderDto = await MapOrderToDtoAsync(order, null, currentRole);
            return Ok(orderDto);
        }

        // 🔹 POST /api/orders/{id}/approve-completion - SystemAdmin xác nhận hoặc từ chối hoàn thành đơn hàng
        [Authorize(Roles = "SystemAdmin")]
        [HttpPost("{id}/approve-completion")]
        public async Task<ActionResult<OrderDto>> ApproveOrderCompletion(int id, [FromBody] ApproveOrderCompletionDto dto)
        {
            var userId = await GetUserIdFromTokenAsync();
            if (userId == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            // Chỉ có thể approve/reject khi đơn hàng ở trạng thái "PendingCompletion"
            if (order.Status != "PendingCompletion")
                return BadRequest("Chỉ có thể xác nhận hoặc từ chối đơn hàng ở trạng thái PendingCompletion.");

            if (dto.Approved)
            {
                // Approve: Chuyển sang Completed và cộng tiền vào ví của EnterpriseAdmin
                order.Status = "Completed";
                order.CompletionApprovedAt = DateTime.UtcNow;
                order.CompletionRejectedAt = null;
                order.CompletionRejectionReason = null;

                await _context.SaveChangesAsync();

                // Tính toán số tiền cho mỗi Enterprise và cộng vào ví của EnterpriseAdmin
                var enterpriseAmounts = order.OrderItems
                    .Where(oi => oi.Product != null)
                    .GroupBy(oi => oi.Product!.EnterpriseId)
                    .Select(g => new
                    {
                        EnterpriseId = g.Key,
                        Amount = g.Sum(oi => oi.Price * oi.Quantity)
                    })
                    .ToList();

                foreach (var enterpriseAmount in enterpriseAmounts)
                {
                    // Tìm EnterpriseAdmin của Enterprise này
                    var enterpriseAdmin = await _context.Users
                        .FirstOrDefaultAsync(u => u.EnterpriseId == enterpriseAmount.EnterpriseId && u.Role == "EnterpriseAdmin");

                    if (enterpriseAdmin != null)
                    {
                        // Cộng tiền vào ví của EnterpriseAdmin
                        var description = $"Thanh toán đơn hàng #{order.Id} - Đơn hàng đã được xác nhận hoàn thành";
                        await _walletService.UpdateUserWalletBalanceAsync(
                            enterpriseAdmin.Id,
                            enterpriseAmount.Amount,
                            description,
                            userId.Value
                        );

                        // Tạo notification cho EnterpriseAdmin về việc cộng tiền
                        _context.Notifications.Add(new Notification
                        {
                            Type = "wallet_deposit",
                            Title = $"Đã cộng {enterpriseAmount.Amount:N0} VND vào ví",
                            Message = $"Đơn hàng #{order.Id} đã được SystemAdmin xác nhận hoàn thành. Số tiền {enterpriseAmount.Amount:N0} VND từ đơn hàng này đã được cộng vào ví của bạn.",
                            UserId = enterpriseAdmin.Id,
                            EnterpriseId = enterpriseAmount.EnterpriseId,
                            OrderId = order.Id,
                            Link = $"/wallet",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // Tạo notification về việc đơn hàng được xác nhận hoàn thành
                var enterpriseIds = order.OrderItems
                    .Where(oi => oi.Product != null)
                    .Select(oi => oi.Product!.EnterpriseId)
                    .Distinct()
                    .ToList();

                foreach (var entId in enterpriseIds)
                {
                    // Tìm EnterpriseAdmin để gửi notification
                    var enterpriseAdmin = await _context.Users
                        .FirstOrDefaultAsync(u => u.EnterpriseId == entId && u.Role == "EnterpriseAdmin");

                    if (enterpriseAdmin != null)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            Type = "order_completion_approved",
                            Title = $"Đơn hàng #{order.Id} đã được xác nhận hoàn thành",
                            Message = $"Đơn hàng #{order.Id} đã được SystemAdmin xác nhận hoàn thành.",
                            UserId = enterpriseAdmin.Id,
                            EnterpriseId = entId,
                            OrderId = order.Id,
                            Link = $"/enterprise-admin?tab=orders",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            else
            {
                // Reject: Quay lại Shipped và lưu lý do từ chối
                if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                    return BadRequest("Vui lòng nhập lý do từ chối.");

                order.Status = "Shipped";
                order.CompletionRejectedAt = DateTime.UtcNow;
                order.CompletionRejectionReason = dto.RejectionReason.Trim();
                order.CompletionApprovedAt = null;

                await _context.SaveChangesAsync();

                // Tạo notification cho EnterpriseAdmin về việc từ chối
                var enterpriseIds = order.OrderItems
                    .Where(oi => oi.Product != null)
                    .Select(oi => oi.Product!.EnterpriseId)
                    .Distinct()
                    .ToList();

                foreach (var entId in enterpriseIds)
                {
                    _context.Notifications.Add(new Notification
                    {
                        Type = "order_completion_rejected",
                        Title = $"Yêu cầu hoàn thành đơn hàng #{order.Id} bị từ chối",
                        Message = $"Yêu cầu hoàn thành đơn hàng #{order.Id} đã bị từ chối. Lý do: {dto.RejectionReason}",
                        EnterpriseId = entId,
                        OrderId = order.Id,
                        Link = $"/orders/{order.Id}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Reload order để trả về đầy đủ thông tin
            await _context.Entry(order).Reference(o => o.User).LoadAsync();
            await _context.Entry(order).Collection(o => o.OrderItems).LoadAsync();
            await _context.Entry(order).Collection(o => o.Payments).LoadAsync();
            
            // Load Product và Enterprise cho OrderItems
            foreach (var item in order.OrderItems)
            {
                await _context.Entry(item).Reference(oi => oi.Product).LoadAsync();
                if (item.Product != null)
                {
                    await _context.Entry(item.Product).Reference(p => p.Enterprise).LoadAsync();
                }
            }

            // Lấy enterpriseId nếu là EnterpriseAdmin
            int? enterpriseId = null;
            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (currentRole == "EnterpriseAdmin")
            {
                var currentUserId = await GetUserIdFromTokenAsync();
                if (currentUserId.HasValue)
                {
                    enterpriseId = (await _context.Users.FindAsync(currentUserId.Value))?.EnterpriseId ?? 0;
                    if (enterpriseId == 0) enterpriseId = null;
                }
            }

            var orderDto = await MapOrderToDtoAsync(order, enterpriseId, currentRole);
            return Ok(orderDto);
        }

        // 🔹 Helper method để tạo notification khi có đơn hàng BankTransfer cần xét duyệt
        private async Task CreateBankTransferNotificationAsync(int orderId)
        {
            // Tạo notification cho SystemAdmin
            var systemAdmins = await _context.Users
                .Where(u => u.Role == "SystemAdmin")
                .ToListAsync();

            foreach (var admin in systemAdmins)
            {
                _context.Notifications.Add(new Notification
                {
                    Type = "bank_transfer_pending",
                    Title = $"Đơn hàng #{orderId} cần xét duyệt chuyển khoản",
                    Message = $"Có đơn hàng #{orderId} thanh toán bằng chuyển khoản ngân hàng cần được xét duyệt. Vui lòng kiểm tra và xác nhận.",
                    UserId = admin.Id,
                    OrderId = orderId,
                    Link = $"/admin?tab=order-management",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        // 🔹 Helper method để tạo notification khi EnterpriseAdmin request completion
        private async Task CreateCompletionRequestNotificationAsync(int orderId, int enterpriseId)
        {
            // Tạo notification cho SystemAdmin (không có EnterpriseId cụ thể, SystemAdmin sẽ thấy tất cả)
            // Hoặc có thể gửi cho tất cả SystemAdmin users
            var systemAdmins = await _context.Users
                .Where(u => u.Role == "SystemAdmin")
                .ToListAsync();

            foreach (var admin in systemAdmins)
            {
                _context.Notifications.Add(new Notification
                {
                    Type = "order_completion_request",
                    Title = $"Yêu cầu xác nhận hoàn thành đơn hàng #{orderId}",
                    Message = $"EnterpriseAdmin đã gửi yêu cầu xác nhận hoàn thành cho đơn hàng #{orderId}. Vui lòng xét duyệt.",
                    UserId = admin.Id, // Gửi cho SystemAdmin cụ thể
                    OrderId = orderId,
                    Link = $"/admin/order-management",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        // 🔹 Helper method để map Order sang OrderDto
        // enterpriseId: Nếu có, chỉ trả về orderItems thuộc enterprise này (cho EnterpriseAdmin)
        // role: Role của user hiện tại (để quyết định có trả về EnterpriseStatuses không)
        private Task<OrderDto> MapOrderToDtoAsync(Order order, int? enterpriseId = null, string? role = null)
        {
            // 🔹 Lấy địa chỉ từ ShippingAddressDetail hoặc ShippingAddress (string)
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

            // 🔹 Filter orderItems: Nếu có enterpriseId, chỉ trả về orderItems thuộc enterprise này
            var orderItemsToReturn = order.OrderItems.AsEnumerable();
            if (enterpriseId.HasValue)
            {
                // Chỉ lấy orderItems có Product thuộc enterprise này
                orderItemsToReturn = order.OrderItems.Where(oi => oi.Product != null && oi.Product.EnterpriseId == enterpriseId.Value);
            }

            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                ShippingAddress = shippingAddress,
                ShippingAddressId = order.ShippingAddressId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                PaymentReference = order.PaymentReference,
                BankTransferRejectionReason = order.BankTransferRejectionReason,
                ShipperId = order.ShipperId,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                DeliveryNotes = order.DeliveryNotes,
                CompletionRequestedAt = order.CompletionRequestedAt,
                CompletionApprovedAt = order.CompletionApprovedAt,
                CompletionRejectedAt = order.CompletionRejectedAt,
                CompletionRejectionReason = order.CompletionRejectionReason,
                Customer = order.User != null ? new CustomerInfoDto
                {
                    Id = order.User.Id,
                    Name = order.User.Name,
                    Email = order.User.Email,
                    PhoneNumber = order.User.PhoneNumber,
                    AvatarUrl = order.User.AvatarUrl,
                    Address = BuildCustomerAddress(order.User)
                } : null,
                OrderItems = orderItemsToReturn.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    EnterpriseId = oi.Product?.EnterpriseId,
                    EnterpriseName = oi.Product?.Enterprise?.Name,
                    EnterpriseImageUrl = oi.Product?.Enterprise?.ImageUrl,
                    ProductName = oi.Product?.Name,
                    ProductImageUrl = oi.Product?.ImageUrl
                }).ToList(),
                Payments = order.Payments.Select(MapPaymentToDto).ToList(),
                EnterpriseStatuses = null // Sẽ được set trong GetOrders và GetOrder
            };
            return Task.FromResult(orderDto);
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
            // Lấy thông tin orderItems với Product và Enterprise
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Enterprise)
                .Where(oi => oi.OrderId == orderId && oi.Product != null)
                .ToListAsync();

            if (!orderItems.Any())
                return;

            // Nhóm orderItems theo EnterpriseId
            var enterpriseGroups = orderItems
                .Where(oi => oi.Product != null)
                .GroupBy(oi => oi.Product!.EnterpriseId)
                .ToList();

            foreach (var group in enterpriseGroups)
            {
                var enterpriseId = group.Key;
                var itemsForEnterprise = group.ToList();
                var productNames = itemsForEnterprise
                    .Select(oi => oi.Product!.Name)
                    .Take(3) // Chỉ lấy 3 sản phẩm đầu tiên để hiển thị
                    .ToList();
                
                var productCount = itemsForEnterprise.Count;
                var productList = productCount <= 3 
                    ? string.Join(", ", productNames)
                    : string.Join(", ", productNames) + $" và {productCount - 3} sản phẩm khác";

                // Tìm EnterpriseAdmin của enterprise này để gửi notification
                var enterpriseAdmin = await _context.Users
                    .FirstOrDefaultAsync(u => u.EnterpriseId == enterpriseId && u.Role == "EnterpriseAdmin");

                _context.Notifications.Add(new Notification
                {
                    Type = "new_order",
                    Title = $"Đơn hàng mới #{orderId}",
                    Message = $"Bạn có đơn hàng mới với {productCount} sản phẩm: {productList}. Vui lòng chấp nhận đơn hàng để SystemAdmin có thể tiếp tục xử lý.",
                    EnterpriseId = enterpriseId,
                    UserId = enterpriseAdmin?.Id, // Gửi cho EnterpriseAdmin cụ thể nếu có
                    OrderId = orderId,
                    Link = $"/enterprise-admin?tab=orders"
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
        [System.ComponentModel.DataAnnotations.RegularExpression("^(Pending|Processing|Shipped|Completed|Cancelled|PendingCompletion)$", 
            ErrorMessage = "Status không hợp lệ. Chỉ chấp nhận: Pending, Processing, Shipped, Completed, Cancelled, PendingCompletion")]
        public string Status { get; set; } = string.Empty;
    }

    // 🔹 DTO cho Request Order Completion (EnterpriseAdmin)
    public class RequestOrderCompletionDto
    {
        public string? Notes { get; set; }
    }

    // 🔹 DTO cho Approve Order Completion (SystemAdmin)
    public class ApproveOrderCompletionDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public bool Approved { get; set; }
        public string? RejectionReason { get; set; }
    }

    // 🔹 DTO cho Confirm Bank Transfer (SystemAdmin)
    public class ConfirmBankTransferDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public bool Confirmed { get; set; }
        public string? RejectionReason { get; set; }
    }
}