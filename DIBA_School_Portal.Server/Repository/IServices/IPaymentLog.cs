using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IPaymentLog
    {
        Task<PaymentLog> RecordPaymentAsync(string studentId, decimal amount, string reference, PaymentGateway gateway, string description);
        Task<PaymentLog?> UpdatePaymentStatusAsync(string reference, PaymentStatus newStatus);
        Task<IEnumerable<PaymentLog>> GetStudentPaymentsAsync(string studentId);
        Task<IEnumerable<PaymentLog>> GetRecentPaymentsAsync(int count = 50);
    }
}
