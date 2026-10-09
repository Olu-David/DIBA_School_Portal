using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Resend;
using System.Net;

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


        public async Task<IEnumerable<AuditLog>> GetLogsAsync(int pageNumber = 1, int pageSize = 50)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Max(1, pageSize);

            return await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(l => l.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task LogActivityAsync(string userId, string action, string details, string ipAddress)
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

            // ✅ Only SuperAdmin + Principal receive the live audit stream
            await _hubContext.Clients
                .Groups(new[] { SignalRGroups.SuperAdmins, SignalRGroups.Principals })
                .SendAsync("ReceiveAuditLog", new
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
