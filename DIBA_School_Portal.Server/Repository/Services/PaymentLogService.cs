using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class PaymentLogService : IPaymentLog
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<DashboardHub> _hubContext;

        public PaymentLogService(AppDbContext context, IHubContext<DashboardHub> hubContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        // 1. Record a new payment (e.g., when initialized or caught via webhook)
        public async Task<PaymentLog> RecordPaymentAsync(string studentId, decimal amount, string reference, PaymentGateway gateway, string description)
        {
            var payment = new PaymentLog
            {
                StudentId = studentId,
                Amount = amount,
                Reference = reference,
                Gateway = gateway,
                Status = PaymentStatus.Pending, // Starts as pending until verified
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentLogs.Add(payment);
            await _context.SaveChangesAsync();

            // Real-time broadcast for a new pending payment entry
            await _hubContext.Clients.All.SendAsync("ReceivePaymentUpdate", payment);

            return payment;
        }

        // 2. Update payment status (e.g., when webhook confirms Success or Failure)
        public async Task<PaymentLog?> UpdatePaymentStatusAsync(string reference, PaymentStatus newStatus)
        {
            var payment = await _context.PaymentLogs
                .FirstOrDefaultAsync(p => p.Reference == reference);

            if (payment == null) return null;

            payment.Status = newStatus;

            if (newStatus == PaymentStatus.Successful)
            {
                payment.VerifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Real-time broadcast notifying dashboards that the status changed (e.g., Success!)
            await _hubContext.Clients.All.SendAsync("ReceivePaymentUpdate", payment);

            return payment;
        }

        // 3. Get all payment logs for a specific student's financial history
        public async Task<IEnumerable<PaymentLog>> GetStudentPaymentsAsync(string studentId)
        {
            return await _context.PaymentLogs
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        // 4. Get recent system-wide payments for the admin/bursary dashboard
        public async Task<IEnumerable<PaymentLog>> GetRecentPaymentsAsync(int count = 50)
        {
            return await _context.PaymentLogs
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
