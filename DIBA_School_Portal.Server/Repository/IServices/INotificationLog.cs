using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface INotificationLog
    {
        Task SendNotificationAsync(string userId, string title, string message, string category );
        Task<IEnumerable<NotificationLog>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
        Task MarkAsReadAsync(Guid notificationId, string userId);
        Task<int> GetUnreadCountAsync(string userId);
    }
}
