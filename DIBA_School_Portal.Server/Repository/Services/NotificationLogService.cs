using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
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
        public async Task SendNotificationAsync(string userId, string title, string message, string category)
        {  var notification = new NotificationLog
            {
                UserId = userId,
                Title = title,
                Message = message,
                Category = category,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationLogs.Add(notification);
            await _context.SaveChangesAsync();

            // ✅ Only that user receives it (personal group created in DashboardHub)
            await _hubContext.Clients
                .Group(SignalRGroups.User(userId))
                .SendAsync("ReceiveNotification", notification);

            // Fallback: also fire through the default IUserIdProvider path
            await _hubContext.Clients
                .User(userId)
                .SendAsync("ReceiveNotification", notification);
        }

        public async Task<IEnumerable<NotificationLog>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
        {
            var query = _context.NotificationLogs.Where(n => n.UserId == userId);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

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

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.NotificationLogs
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
    }
}