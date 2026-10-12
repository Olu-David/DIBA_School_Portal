using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;

using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using DIBA_School_Portal.Server.Services.Background;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DIBA_School_Portal.Server.Repository.Services.Background
{

    public class NotificationDispatchService : INotificationDispatchService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBackgroundEmailQueue _emailQueue;
        private readonly IBackgroundSmsQueue _smsQueue;
        private readonly IAuditLog _auditLog;
        private readonly ILogger<NotificationDispatchService> _logger;

        public NotificationDispatchService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IBackgroundEmailQueue emailQueue,
            IBackgroundSmsQueue smsQueue,
            IAuditLog auditLog,
            ILogger<NotificationDispatchService> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailQueue = emailQueue;
            _smsQueue = smsQueue;
            _auditLog = auditLog;
            _logger = logger;
        }

        // ==========================================================
        // EMAIL
        // ==========================================================
        public async Task<ServiceResponse<SendQueuedResponse>> SendEmailAsync(
            SendEmailRequest request, string currentUserId, string ip)
        {
            var response = new ServiceResponse<SendQueuedResponse>();

            if (string.IsNullOrWhiteSpace(request.Subject) ||
                string.IsNullOrWhiteSpace(request.HtmlBody))
            {
                response.Success = false;
                response.Message = "Subject and body are required.";
                response.StatusCode = 400;
                return response;
            }

            var recipients = await ResolveRecipientsAsync(
                request.Target, request.ParentUserIds,
                request.ClassId, request.Section);

            if (recipients.Count == 0)
            {
                response.Success = false;
                response.Message = "No recipients matched.";
                response.StatusCode = 400;
                return response;
            }

            var queueId = Guid.NewGuid().ToString("N")[..8];
            int queued = 0;

            foreach (var r in recipients)
            {
                if (string.IsNullOrWhiteSpace(r.Email)) continue;

                _emailQueue.Enqueue(new EmailJob
                {
                    QueueId = queueId,
                    ToEmail = r.Email!,
                    Subject = request.Subject.Trim(),
                    HtmlBody = request.HtmlBody,
                    RecipientName = r.FullName,
                });
                queued++;
            }

            await _auditLog.LogActivityAsync(
                currentUserId,
                "Bulk Email Queued",
                $"Queue {queueId}: '{request.Subject}' → {queued} recipient(s) [{request.Target}].",
                ip);

            _logger.LogInformation("Bulk email queued: {Count} recipients, queue {QueueId}",
                queued, queueId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = $"Queued for {queued} recipient(s).";
            response.Data = new SendQueuedResponse
            {
                QueueId = queueId,
                RecipientCount = queued,
                Message = "Emails are being sent in the background."
            };
            return response;
        }

        // ==========================================================
        // SMS
        // ==========================================================
        public async Task<ServiceResponse<SendQueuedResponse>> SendSmsAsync(
            SendSmsRequest request, string currentUserId, string ip)
        {
            var response = new ServiceResponse<SendQueuedResponse>();

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                response.Success = false;
                response.Message = "Message is required.";
                response.StatusCode = 400;
                return response;
            }

            if (request.Message.Length > 480)
            {
                response.Success = false;
                response.Message = "SMS must not exceed 480 characters (3 segments).";
                response.StatusCode = 400;
                return response;
            }

            var recipients = await ResolveRecipientsAsync(
                request.Target, request.ParentUserIds,
                request.ClassId, request.Section);

            if (recipients.Count == 0)
            {
                response.Success = false;
                response.Message = "No recipients matched.";
                response.StatusCode = 400;
                return response;
            }

            var queueId = Guid.NewGuid().ToString("N")[..8];
            int queued = 0;

            foreach (var r in recipients)
            {
                if (string.IsNullOrWhiteSpace(r.Phone)) continue;

                _smsQueue.Enqueue(new SmsJob
                {
                    QueueId = queueId,
                    ToPhone = r.Phone!,
                    Message = request.Message.Trim(),
                    RecipientName = r.FullName,
                });
                queued++;
            }

            await _auditLog.LogActivityAsync(
                currentUserId,
                "Bulk SMS Queued",
                $"Queue {queueId}: {queued} recipient(s) [{request.Target}].",
                ip);

            _logger.LogInformation("Bulk SMS queued: {Count} recipients, queue {QueueId}",
                queued, queueId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = $"Queued for {queued} recipient(s).";
            response.Data = new SendQueuedResponse
            {
                QueueId = queueId,
                RecipientCount = queued,
                Message = "SMS are being sent in the background."
            };
            return response;
        }

        // ==========================================================
        // Resolve recipients based on target
        // ==========================================================
        private async Task<List<RecipientDto>> ResolveRecipientsAsync(
            string target, List<string>? parentIds, Guid? classId, string? section)
        {
            target = (target ?? "List").Trim();

            IQueryable<ApplicationUser> query = _userManager.Users;

            // Only parents — filter by role is expensive in one query,
            // so we resolve via the ParentStudents links instead
            var parentUserIds = _context.ParentStudents
                .AsNoTracking()
                .Select(ps => ps.ParentUserId)
                .Distinct();

            query = query.Where(u => parentUserIds.Contains(u.Id) && u.IsActive && !u.IsDeleted);

            // ---- Apply target filters ----
            if (target.Equals("List", StringComparison.OrdinalIgnoreCase))
            {
                if (parentIds == null || parentIds.Count == 0)
                    return new List<RecipientDto>();

                query = query.Where(u => parentIds.Contains(u.Id));
            }
            else if (target.Equals("Class", StringComparison.OrdinalIgnoreCase))
            {
                if (!classId.HasValue) return new List<RecipientDto>();

                var studentUserIds = _context.Students
                    .AsNoTracking()
                    .Where(s => s.SchoolClassId == classId.Value)
                    .Select(s => s.Id);

                var parentIdsForClass = _context.ParentStudents
                    .AsNoTracking()
                    .Where(ps => studentUserIds.Contains(ps.StudentProfileId))
                    .Select(ps => ps.ParentUserId)
                    .Distinct();

                query = query.Where(u => parentIdsForClass.Contains(u.Id));
            }
            else if (target.Equals("Section", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(section) ||
                    !Enum.TryParse<AcademicSection>(section, true, out var parsed))
                    return new List<RecipientDto>();

                var studentUserIds = _context.Students
                    .AsNoTracking()
                    .Where(s => s.SchoolClass.AcademicSection == parsed)
                    .Select(s => s.Id);

                var parentIdsForSection = _context.ParentStudents
                    .AsNoTracking()
                    .Where(ps => studentUserIds.Contains(ps.StudentProfileId))
                    .Select(ps => ps.ParentUserId)
                    .Distinct();

                query = query.Where(u => parentIdsForSection.Contains(u.Id));
            }
            // "All" → no additional filter

            return await query
                .Select(u => new RecipientDto
                {
                    UserId = u.Id,
                    Email = u.Email,
                    Phone = u.PhoneNumber,
                    FullName = (u.FirstName ?? "") + " " + (u.LastName ?? ""),
                })
                .ToListAsync();
        }

        private class RecipientDto
        {
            public string UserId { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string FullName { get; set; } = string.Empty;
        }
    }
}