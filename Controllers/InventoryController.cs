using System;
using System.Collections.Generic;
using System.Linq;
using GiaLaiOCOP.Api.Data;
using GiaLaiOCOP.Api.Dtos;
using GiaLaiOCOP.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GiaLaiOCOP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "EnterpriseAdmin,SystemAdmin")]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy lịch sử kho
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<InventoryHistoryDto>>> GetHistory(
            [FromQuery] int? productId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 50 : pageSize;
            pageSize = pageSize > 200 ? 200 : pageSize;

            var query = _context.InventoryHistories
                .Include(h => h.Product)
                .Include(h => h.Enterprise)
                .Include(h => h.CreatedByUser)
                .AsQueryable();

            if (user.Role == "EnterpriseAdmin")
            {
                if (user.EnterpriseId == null)
                    return Forbid("Bạn không thuộc doanh nghiệp nào.");

                query = query.Where(h => h.EnterpriseId == user.EnterpriseId.Value);
            }

            if (productId.HasValue)
            {
                query = query.Where(h => h.ProductId == productId.Value);
            }

            var totalItems = await query.CountAsync();

            var histories = await query
                .OrderByDescending(h => h.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new
            {
                items = histories.Select(MapHistoryToDto),
                page,
                pageSize,
                totalItems,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return Ok(response);
        }

        /// <summary>
        /// Điều chỉnh tồn kho
        /// </summary>
        [HttpPost("adjust")]
        public async Task<ActionResult<InventoryHistoryDto>> AdjustInventory([FromBody] AdjustInventoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Quantity == 0)
                return BadRequest("Số lượng điều chỉnh phải khác 0.");

            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var product = await _context.Products
                .Include(p => p.Enterprise)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

            if (product == null)
                return NotFound("Không tìm thấy sản phẩm.");

            if (user.Role == "EnterpriseAdmin")
            {
                if (user.EnterpriseId == null)
                    return Forbid("Bạn không thuộc doanh nghiệp nào.");

                if (product.EnterpriseId != user.EnterpriseId.Value)
                    return Forbid("Bạn chỉ có thể điều chỉnh tồn kho cho sản phẩm thuộc doanh nghiệp của mình.");
            }

            var previousQuantity = product.StockQuantity;
            var newQuantity = previousQuantity + dto.Quantity;

            if (newQuantity < 0)
                return BadRequest("Số lượng mới không thể nhỏ hơn 0.");

            var threshold = dto.LowStockThreshold <= 0 ? 1 : dto.LowStockThreshold;

            product.StockQuantity = newQuantity;
            product.StockStatus = CalculateStockStatus(newQuantity, threshold);
            product.UpdatedAt = DateTime.UtcNow;

            var history = new InventoryHistory
            {
                ProductId = product.Id,
                EnterpriseId = product.EnterpriseId,
                Type = dto.Type,
                Quantity = dto.Quantity,
                PreviousQuantity = previousQuantity,
                NewQuantity = newQuantity,
                Reason = dto.Reason,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = user.Id
            };

            _context.InventoryHistories.Add(history);

            await _context.SaveChangesAsync();

            if (newQuantity <= threshold)
            {
                await CreateLowStockNotificationAsync(product, newQuantity);
            }

            await _context.Entry(history).Reference(h => h.Product).LoadAsync();
            await _context.Entry(history).Reference(h => h.Enterprise).LoadAsync();
            await _context.Entry(history).Reference(h => h.CreatedByUser).LoadAsync();

            return Ok(MapHistoryToDto(history));
        }

        private static string CalculateStockStatus(int quantity, int threshold)
        {
            if (quantity == 0)
                return "OutOfStock";

            if (quantity <= threshold)
                return "LowStock";

            return "InStock";
        }

        private async Task CreateLowStockNotificationAsync(Product product, int quantity)
        {
            _context.Notifications.Add(new Notification
            {
                Type = "low_stock",
                Title = $"Tồn kho thấp: {product.Name}",
                Message = $"Số lượng còn lại: {quantity}",
                EnterpriseId = product.EnterpriseId,
                ProductId = product.Id,
                Link = $"/products/{product.Id}"
            });

            await _context.SaveChangesAsync();
        }

        private InventoryHistoryDto MapHistoryToDto(InventoryHistory history)
        {
            return new InventoryHistoryDto
            {
                Id = history.Id,
                ProductId = history.ProductId,
                ProductName = history.Product?.Name ?? string.Empty,
                EnterpriseId = history.EnterpriseId,
                EnterpriseName = history.Enterprise?.Name ?? string.Empty,
                Type = history.Type,
                Quantity = history.Quantity,
                PreviousQuantity = history.PreviousQuantity,
                NewQuantity = history.NewQuantity,
                Reason = history.Reason,
                CreatedAt = history.CreatedAt,
                CreatedByUserId = history.CreatedByUserId,
                CreatedByName = history.CreatedByUser?.Name
            };
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(claimValue))
                return null;

            if (int.TryParse(claimValue, out var userId))
                return await _context.Users.FindAsync(userId);

            if (claimValue.Contains("@"))
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == claimValue);
            }

            return null;
        }
    }
}

