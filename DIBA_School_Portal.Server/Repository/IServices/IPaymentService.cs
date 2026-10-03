using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IPaymentService
    {

               Task<ServiceResponse<string>> RecordPaymentAsync(CreatePaymentDto paymentDto);
            Task<ServiceResponse<IEnumerable<StudentPaymentDto>>> GetPaymentsByStudentAsync(Guid studentProfileId);
            Task<ServiceResponse<StudentPaymentDto>> GetPaymentByIdAsync(Guid paymentId);
            Task<ServiceResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus status);
        Task<ServiceResponse<StudentBillResponseDto>> GetStudentBillBreakdownAsync(Guid studentProfileId, Guid termId);

            // Add these for Flutterwave/Gateway handling:
            Task<ServiceResponse<StudentPaymentDto>> GetPaymentByReferenceAsync(string referenceNumber);
            Task<ServiceResponse<bool>> VerifyAndCompleteFlutterwavePaymentAsync(string transactionId, string referenceNumber);
          
    }
 }

