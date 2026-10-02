using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // any logged-in user
    public class NotificationLogController : ControllerBase
    {
        private readonly INotificationLog _notification;

        public NotificationLogController(INotificationLog notification)
        {
            _notification = notification;
        }

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException();

        [HttpGet("my")]
        public async Task<IActionResult> GetMyNotifications([FromQuery] bool unreadOnly = false)
        {
            var data = await _notification.GetUserNotificationsAsync(CurrentUserId, unreadOnly);
            return Ok(data);
        }

        [HttpGet("my/unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _notification.GetUnreadCountAsync(CurrentUserId);
            return Ok(new { unreadCount = count });
        }

        [HttpPost("mark-read/{id:guid}")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            await _notification.MarkAsReadAsync(id, CurrentUserId);
            return NoContent();
        }
    }
}