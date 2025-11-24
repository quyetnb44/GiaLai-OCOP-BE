using System.Linq;
using System.Collections.Generic;
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
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách thông báo (filter unread/type)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications(
            [FromQuery] bool unreadOnly = false,
            [FromQuery] string? type = null)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var query = _context.Notifications
                .Where(n =>
                    (n.UserId != null && n.UserId == user.Id) ||
                    (user.EnterpriseId != null && n.EnterpriseId == user.EnterpriseId));

            if (unreadOnly)
                query = query.Where(n => !n.Read);

            if (!string.IsNullOrWhiteSpace(type))
            {
                var types = type.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLowerInvariant())
                    .Distinct()
                    .ToList();

                if (types.Count > 0)
                {
                    query = query.Where(n => types.Contains(n.Type.ToLower()));
                }
            }

            var notifications = await query
                .Include(n => n.Product)
                .Include(n => n.Order)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var dtos = notifications.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Đánh dấu thông báo đã đọc
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound("Không tìm thấy thông báo.");

            if (!HasAccess(notification, user))
                return Forbid();

            if (!notification.Read)
            {
                notification.Read = true;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo đã đọc
        /// </summary>
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var notifications = await _context.Notifications
                .Where(n =>
                    !n.Read &&
                    ((n.UserId != null && n.UserId == user.Id) ||
                     (user.EnterpriseId != null && n.EnterpriseId == user.EnterpriseId)))
                .ToListAsync();

            if (!notifications.Any())
                return NoContent();

            foreach (var notification in notifications)
            {
                notification.Read = true;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Xóa thông báo
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound("Không tìm thấy thông báo.");

            if (!HasAccess(notification, user))
                return Forbid();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private NotificationDto MapToDto(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                Read = notification.Read,
                CreatedAt = notification.CreatedAt,
                Link = notification.Link,
                EnterpriseId = notification.EnterpriseId,
                UserId = notification.UserId,
                ProductId = notification.ProductId,
                OrderId = notification.OrderId,
                ProductName = notification.Product?.Name,
                OrderCode = notification.OrderId.HasValue ? $"#{notification.OrderId.Value}" : null
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

        private static bool HasAccess(Notification notification, User user)
        {
            if (notification.UserId.HasValue && notification.UserId.Value == user.Id)
                return true;

            if (notification.EnterpriseId.HasValue && user.EnterpriseId.HasValue &&
                notification.EnterpriseId.Value == user.EnterpriseId.Value)
            {
                return true;
            }

            return false;
        }
    }
}

