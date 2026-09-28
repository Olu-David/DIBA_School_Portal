using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class AuditLogService : IAuditLog
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public AuditLogService(AppDbContext context, IHubContext<DashboardHub> hubContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        public Task<IEnumerable<AuditLog>> GetLogsAsync(int pageNumber = 1, int pageSize = 50)
        {
            throw new NotImplementedException();
        }

        public async Task  LogActivityAsync(string userId, string action, string details, string ipAddress)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                Details = details,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            // <--- 2. Broadcast the new log live via WebSockets!
            await _hubContext.Clients.All.SendAsync("ReceiveAuditLog", new
            {
                timestamp = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                userId = log.UserId ?? "System/Guest",
                action = log.Action,
                details = log.Details,
                ipAddress = log.IpAddress,
                tenantId = log.TenantId.HasValue ? log.TenantId.ToString() : "Global"
            });
        }
    }
}
