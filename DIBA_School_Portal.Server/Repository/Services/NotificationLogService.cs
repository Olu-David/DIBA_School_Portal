using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class NotificationLogService : INotificationLog
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public NotificationLogService(AppDbContext context, IHubContext<DashboardHub> hubContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }


    // 1. Send/Create a notification and push it live via SignalR
        public async Task SendNotificationAsync(string userId, string title, string message, string channel = "InApp", string category = "General")
        {
            var notification = new NotificationLog
            {
                UserId = userId,
                Title = title,
                Message = message,
                Channel = channel,
                Category = category,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationLogs.Add(notification);
            await _context.SaveChangesAsync();

            // Real-time push to the specific user's connected dashboard session
            // (Assumes you map user connections or broadcast to a specific group/client)
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification);
        }

        // 2. Fetch a user's notifications (all or unread only)
        public async Task<IEnumerable<NotificationLog>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
        {
            var query = _context.NotificationLogs
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50) // Limit to last 50 for performance
                .ToListAsync();
        }

        // 3. Mark a specific notification as read when clicked
        public async Task MarkAsReadAsync(Guid notificationId, string userId)
        {
            var notification = await _context.NotificationLogs
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // 4. Get total unread count for the header badge (e.g., the red '3' on the bell icon)
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.NotificationLogs
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
    }
}