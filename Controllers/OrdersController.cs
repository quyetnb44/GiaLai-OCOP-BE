using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Models;
using GiaLaiOCOP.Api.Dtos;
using System.Security.Claims;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // tất cả user phải đăng nhập
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public OrdersController(AppDbContext context) => _context = context;

        // GET /api/orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // ✅ Check claim an toàn
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return BadRequest("Invalid user id in token.");

            if (role == "Customer")
            {
                // Customer chỉ xem đơn của chính mình
                return await _context.Orders
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .Where(o => o.UserId == userId)
                    .ToListAsync();
            }
            else if (role == "EnterpriseAdmin")
            {
                var enterpriseId = (await _context.Users.FindAsync(userId))?.EnterpriseId ?? 0;
                return await _context.Orders
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .Where(o => o.OrderItems.Any(oi => oi.Product.EnterpriseId == enterpriseId))
                    .ToListAsync();
            }
            else if (role == "SystemAdmin")
            {
                // SystemAdmin xem tất cả
                return await _context.Orders
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .ToListAsync();
            }

            return Forbid();
        }

        // POST /api/orders (chỉ Customer)
        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(CreateOrderDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // ✅ Check claim an toàn
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return BadRequest("Invalid user id in token.");

            if (string.IsNullOrEmpty(dto.ShippingAddress))
                return BadRequest("ShippingAddress is required.");

            var order = new Order
            {
                UserId = userId,
                ShippingAddress = dto.ShippingAddress,
                OrderDate = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            decimal total = 0;
            foreach (var item in dto.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null) return BadRequest($"Sản phẩm ID {item.ProductId} không tồn tại.");

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = product.Price
                };
                total += product.Price * item.Quantity;
                _context.OrderItems.Add(orderItem);
            }

            order.TotalAmount = total;
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, order);
        }
    }
}
