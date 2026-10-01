using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        #region Constructor
        public PaymentService(
            AppDbContext context,
            ILogger<PaymentService> logger,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        #endregion

        #region RecordPaymentAsync
      
        public async Task<ServiceResponse<string>> RecordPaymentAsync(CreatePaymentDto paymentDto)
        {
            var response = new ServiceResponse<string>();
            try
            {
                if (paymentDto == null)
                {
                    response.Success = false;
                    response.Message = "Payment payload cannot be null.";
                    response.StatusCode = 400;
                    return response;
                }

                // Verify student profile exists and get their SchoolClassId
                var studentProfile = await _context.StudentProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == paymentDto.StudentProfileId);

                if (studentProfile == null)
                {
                    response.Success = false;
                    response.Message = "Student profile not found.";
                    response.StatusCode = 404;
                    return response;
                }

                // Verify session and term validity
                var termExists = await _context.SchoolTerms
                    .AnyAsync(t => t.Id == paymentDto.TermId && t.AcademicSessionId == paymentDto.AcademicSessionId);
                if (!termExists)
                {
                    response.Success = false;
                    response.Message = "The selected term does not belong to the specified academic session.";
                    response.StatusCode = 400;
                    return response;
                }

                // Fetch the ClassFeePrice for the student's class and term (looking for tuition/school fee first)
                var classFeePrice = await _context.ClassFeePrices
                    .Include(cfp => cfp.Fee)
                    .FirstOrDefaultAsync(cfp => cfp.SchoolClassId == studentProfile.SchoolClassId &&
                                                cfp.Fee.TermId == paymentDto.TermId &&
                                                (cfp.Fee.FeeName.ToLower().Contains("tuition") || cfp.Fee.FeeName.ToLower().Contains("school fee")));

                // Fallback to any fee price for that class and term if no explicit tuition label is found
                if (classFeePrice == null)
                {
                    classFeePrice = await _context.ClassFeePrices
                        .Include(cfp => cfp.Fee)
                        .FirstOrDefaultAsync(cfp => cfp.SchoolClassId == studentProfile.SchoolClassId &&
                                                    cfp.Fee.TermId == paymentDto.TermId);
                }

                // Validate that the payment amount meets the minimum required amount from ClassFeePrice
                if (classFeePrice != null && paymentDto.AmountPaid < classFeePrice.MinimumAmountToPay)
                {
                    response.Success = false;
                    response.Message = $"Payment amount ({paymentDto.AmountPaid:N2}) is less than the minimum required amount ({classFeePrice.MinimumAmountToPay:N2}).";
                    response.StatusCode = 400;
                    return response;
                }

                // SECURELY GENERATE REFERENCE ON THE BACKEND
                string secureReference = $"DIBA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

                var payment = new StudentPayment
                {
                    Id = Guid.NewGuid(),
                    StudentProfileId = paymentDto.StudentProfileId,
                    AmountPaid = paymentDto.AmountPaid,
                    ReferenceNumber = secureReference,
                    Gateway = paymentDto.Gateway,
                    Status = PaymentStatus.Pending,
                    AcademicSessionId = paymentDto.AcademicSessionId,
                    TermId = paymentDto.TermId,
                    PaymentDate = DateTime.UtcNow,
                    ProofOfPaymentUrl = paymentDto.ProofOfPaymentUrl?.Trim()
                };

                await _context.StudentPayments.AddAsync(payment);
                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Payment initialized successfully.";
                response.StatusCode = 200;
                response.Data = secureReference;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording student payment.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
            }
            return response;
        }
        
        #endregion

        #region GetPaymentsByStudentAsync
        public async Task<ServiceResponse<IEnumerable<StudentPaymentDto>>> GetPaymentsByStudentAsync(Guid studentProfileId)
        {
            var response = new ServiceResponse<IEnumerable<StudentPaymentDto>>();
            try
            {
                var payments = await _context.StudentPayments
                    .AsNoTracking()
                    .Include(p => p.StudentProfile)
                    .Include(p => p.AcademicSession)
                    .Include(p => p.Term)
                    .Where(p => p.StudentProfileId == studentProfileId)
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new StudentPaymentDto
                    {
                        Id = p.Id,
                        StudentProfileId = p.StudentProfileId,
                        StudentName = p.StudentProfile != null ? $"{p.StudentProfile.FirstName} {p.StudentProfile.LastName}" : "N/A",
                        AmountPaid = p.AmountPaid,
                        ReferenceNumber = p.ReferenceNumber,
                        Gateway = p.Gateway,
                        Status = p.Status,
                        SessionName = p.AcademicSession != null ? p.AcademicSession.Name : "N/A",
                        TermName = p.Term != null ? p.Term.Name : "N/A",
                        PaymentDate = p.PaymentDate,
                        ProofOfPaymentUrl = p.ProofOfPaymentUrl
                    })
                    .ToListAsync();

                response.Success = true;
                response.Data = payments;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payments for student profile: {StudentProfileId}", studentProfileId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region GetPaymentByIdAsync
        public async Task<ServiceResponse<StudentPaymentDto>> GetPaymentByIdAsync(Guid paymentId)
        {
            var response = new ServiceResponse<StudentPaymentDto>();
            try
            {
                var p = await _context.StudentPayments
                    .AsNoTracking()
                    .Include(p => p.StudentProfile)
                    .Include(p => p.AcademicSession)
                    .Include(p => p.Term)
                    .FirstOrDefaultAsync(x => x.Id == paymentId);

                if (p == null)
                {
                    response.Success = false;
                    response.Message = "Payment record not found.";
                    response.StatusCode = 404;
                    return response;
                }

                response.Data = new StudentPaymentDto
                {
                    Id = p.Id,
                    StudentProfileId = p.StudentProfileId,
                    StudentName = p.StudentProfile != null ? $"{p.StudentProfile.FirstName} {p.StudentProfile.LastName}" : "N/A",
                    AmountPaid = p.AmountPaid,
                    ReferenceNumber = p.ReferenceNumber,
                    Gateway = p.Gateway,
                    Status = p.Status,
                    SessionName = p.AcademicSession != null ? p.AcademicSession.Name : "N/A",
                    TermName = p.Term != null ? p.Term.Name : "N/A",
                    PaymentDate = p.PaymentDate,
                    ProofOfPaymentUrl = p.ProofOfPaymentUrl
                };
                response.Success = true;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment by ID: {PaymentId}", paymentId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region UpdatePaymentStatusAsync
        public async Task<ServiceResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus status)
        {
            var response = new ServiceResponse<bool>();
            try
            {
                var payment = await _context.StudentPayments.FirstOrDefaultAsync(p => p.Id == paymentId);
                if (payment == null)
                {
                    response.Success = false;
                    response.Message = "Payment record not found.";
                    response.StatusCode = 404;
                    return response;
                }

                payment.Status = status;
                _context.StudentPayments.Update(payment);
                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Payment status updated successfully.";
                response.StatusCode = 200;
                response.Data = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for ID: {PaymentId}", paymentId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region GetPaymentByReferenceAsync
        public async Task<ServiceResponse<StudentPaymentDto>> GetPaymentByReferenceAsync(string referenceNumber)
        {
            var response = new ServiceResponse<StudentPaymentDto>();
            try
            {
                var p = await _context.StudentPayments
                    .AsNoTracking()
                    .Include(p => p.StudentProfile)
                    .Include(p => p.AcademicSession)
                    .Include(p => p.Term)
                    .FirstOrDefaultAsync(x => x.ReferenceNumber == referenceNumber);

                if (p == null)
                {
                    response.Success = false;
                    response.Message = "Payment record with this reference not found.";
                    response.StatusCode = 404;
                    return response;
                }

                response.Data = new StudentPaymentDto
                {
                    Id = p.Id,
                    StudentProfileId = p.StudentProfileId,
                    StudentName = p.StudentProfile != null ? $"{p.StudentProfile.FirstName} {p.StudentProfile.LastName}" : "N/A",
                    AmountPaid = p.AmountPaid,
                    ReferenceNumber = p.ReferenceNumber,
                    Gateway = p.Gateway,
                    Status = p.Status,
                    SessionName = p.AcademicSession != null ? p.AcademicSession.Name : "N/A",
                    TermName = p.Term != null ? p.Term.Name : "N/A",
                    PaymentDate = p.PaymentDate,
                    ProofOfPaymentUrl = p.ProofOfPaymentUrl
                };
                response.Success = true;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment by reference: {Ref}", referenceNumber);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region VerifyAndCompleteFlutterwavePaymentAsync
        public async Task<ServiceResponse<bool>> VerifyAndCompleteFlutterwavePaymentAsync(string transactionId, string referenceNumber)
        {
            var response = new ServiceResponse<bool>();
            try
            {
                var paymentRecord = await _context.StudentPayments
                    .FirstOrDefaultAsync(p => p.ReferenceNumber == referenceNumber);

                if (paymentRecord == null)
                {
                    response.Success = false;
                    response.Message = "Local payment record matching reference was not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var secretKey = _configuration["Flutterwave:SecretKey"];
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);

                string url = $"https://api.flutterwave.com/v3/transactions/{transactionId}/verify";
                var fwResponse = await _httpClient.GetAsync(url);

                if (!fwResponse.IsSuccessStatusCode)
                {
                    response.Success = false;
                    response.Message = "Failed to communicate with Flutterwave verification server.";
                    response.StatusCode = 400;
                    return response;
                }

                var jsonResult = await fwResponse.Content.ReadFromJsonAsync<FlutterwaveApiResponseWrapper>();

                if (jsonResult?.Data == null || jsonResult.Data.Status.ToLower() != "successful")
                {
                    paymentRecord.Status = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();

                    response.Success = false;
                    response.Message = "Transaction was not successful on Flutterwave.";
                    response.StatusCode = 400;
                    return response;
                }

                // Ensure currency matches your expected currency (e.g., NGN)
                if (jsonResult.Data.Currency != "NGN")
                {
                    response.Success = false;
                    response.Message = "Invalid transaction currency.";
                    response.StatusCode = 400;
                    return response;
                }

                if (jsonResult.Data.Amount < paymentRecord.AmountPaid)
                {
                    response.Success = false;
                    response.Message = "Paid amount is less than the required school fee amount.";
                    response.StatusCode = 400;
                    return response;
                }

                // 1. Mark payment as successful and save to DB first
                paymentRecord.Status = PaymentStatus.Successful;
                _context.StudentPayments.Update(paymentRecord);
                await _context.SaveChangesAsync();

                // 2. Fetch the student's enrollment for this term
                var enrollment = await _context.StudentEnrollments
                    .FirstOrDefaultAsync(e => e.StudentProfileId == paymentRecord.StudentProfileId && e.TermId == paymentRecord.TermId);

                if (enrollment != null)
                {
                    // 3. Calculate total successful payments made by this student for this term across all transactions
                    var totalPaidSoFar = await _context.StudentPayments
                        .Where(p => p.StudentProfileId == paymentRecord.StudentProfileId &&
                                    p.TermId == paymentRecord.TermId &&
                                    p.Status == PaymentStatus.Successful)
                        .SumAsync(p => p.AmountPaid);

                    // 4. Fetch the core tuition/school fee item to check its minimum required threshold
                    var tuitionFee = await _context.Fees
                        .FirstOrDefaultAsync(f => f.SchoolClassId == enrollment.SchoolClassId &&
                                                   f.TermId == paymentRecord.TermId &&
                                                   (f.FeeName.ToLower().Contains("tuition") || f.FeeName.ToLower().Contains("school fee")));

                    // Fallback to the first available fee item if no explicit "tuition" label is found
                    if (tuitionFee == null)
                    {
                        tuitionFee = await _context.Fees
                            .FirstOrDefaultAsync(f => f.ClassLevelId == enrollment.SchoolClassId && f.TermId == paymentRecord.TermId);
                    }

                    // 5. Calculate total required fee across ALL fee items combined (for full clearance / isfu check)
                    var totalRequiredFee = await _context.Fees
                        .Where(f => f.ClassLevelId == enrollment.SchoolClassId && f.TermId == paymentRecord.TermId)
                        .SumAsync(f => (decimal?)f.Amount) ?? 0m;

                    // 6. Evaluate Activation Status based on core fee minimum
                    if (tuitionFee != null)
                    {
                        if (totalPaidSoFar < tuitionFee.MinimumAmountToPay)
                        {
                            enrollment.Status = StudentEnrollmentStatus.Pending;
                            enrollment.IsActive = false;
                        }
                        else
                        {
                            enrollment.Status = StudentEnrollmentStatus.Active;
                            enrollment.IsActive=true;   
                        }
                    }

                    // 7. Evaluate Full Payment Status (isfu) for results access
                    if (totalRequiredFee > 0 && totalPaidSoFar >= totalRequiredFee)
                    {
                        enrollment.IsFullyPaid = true; // Fully paid -> Unlocks results portal
                        enrollment.EnrolledAt = DateTime.UtcNow;
                    }
                    else
                    {
                        enrollment.IsFullyPaid = false; // Balance still pending
                    }

                    _context.StudentEnrollments.Update(enrollment);
                    await _context.SaveChangesAsync();
                }

                response.Success = true;
                response.Message = "Flutterwave payment verified and marked as successful.";
                response.StatusCode = 200;
                response.Data = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Flutterwave transaction {TxId}", transactionId);
                response.Success = false;
                response.Message = $"An error occurred during verification: {ex.Message}";
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region HasStudentMetMinimumFeeRequirementAsync
        public async Task<ServiceResponse<bool>> HasStudentMetMinimumFeeRequirementAsync(Guid studentProfileId, Guid termId, decimal minimumPercentage = 0.50m)
        {
            var response = new ServiceResponse<bool>();
            try
            {
                var studentProfile = await _context.StudentProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == studentProfileId);

                if (studentProfile == null)
                {
                    response.Success = false;
                    response.Message = "Student profile not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var expectedFee = await _context.Fees
                    .Where(f => f.SchoolClassId == studentProfile.SchoolClassId && f.TermId == termId)
                    .SumAsync(f => (decimal?)f.Amount) ?? 0m;

                if (expectedFee <= 0)
                {
                    response.Data = true;
                    response.Success = true;
                    return response;
                }

                var totalPaid = await _context.StudentPayments
                    .Where(p => p.StudentProfileId == studentProfileId &&
                                p.TermId == termId &&
                                p.Status == PaymentStatus.Successful)
                    .SumAsync(p => (decimal?)p.AmountPaid) ?? 0m;

                decimal requiredMinimum = expectedFee * minimumPercentage;
                bool hasMetRequirement = totalPaid >= requiredMinimum;

                response.Data = hasMetRequirement;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = hasMetRequirement
                    ? "Minimum payment requirement met."
                    : $"Access restricted. You must pay at least {(minimumPercentage * 100)}% of your school fees to view results.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking fee requirement for student {StudentId}", studentProfileId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region GetStudentComprehensiveBalanceAsync (Arrears + Current Term)
        public async Task<ServiceResponse<StudentComprehensiveBalanceDto>> GetStudentComprehensiveBalanceAsync(Guid studentProfileId, Guid currentTermId)
        {
            var response = new ServiceResponse<StudentComprehensiveBalanceDto>();
            try
            {
                var studentProfile = await _context.StudentProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == studentProfileId);

                if (studentProfile == null)
                {
                    response.Success = false;
                    response.Message = "Student profile not found.";
                    response.StatusCode = 404;
                    return response;
                }

                // 1. Total expected fees across all terms for this student's class history
                var allExpectedFees = await _context.Fees
                    .Where(f => f.SchoolClassId == studentProfile.SchoolClassId)
                    .SumAsync(f => (decimal?)f.Amount) ?? 0m;

                // 2. Total successful payments made by this student across all time
                var totalAllTimePaid = await _context.StudentPayments
                    .Where(p => p.StudentProfileId == studentProfileId &&
                                p.Status == PaymentStatus.Successful)
                    .SumAsync(p => (decimal?)p.AmountPaid) ?? 0m;

                // 3. Cumulative overall balance (Previous Arrears + Current Term Balance)
                decimal totalBalanceRemaining = Math.Max(0m, allExpectedFees - totalAllTimePaid);

                // 4. Specific current term calculation (for breakdown)
                var currentTermExpectedFee = await _context.Fees
                    .Where(f => f.SchoolClassId == studentProfile.SchoolClassId && f.TermId == currentTermId)
                    .SumAsync(f => (decimal?)f.Amount) ?? 0m;

                var currentTermPaid = await _context.StudentPayments
                    .Where(p => p.StudentProfileId == studentProfileId &&
                                p.TermId == currentTermId &&
                                p.Status == PaymentStatus.Successful)
                    .SumAsync(p => (decimal?)p.AmountPaid) ?? 0m;

                decimal currentTermBalance = Math.Max(0m, currentTermExpectedFee - currentTermPaid);

                // 5. Arrears is everything owed minus what belongs to the current term
                decimal previousArrears = Math.Max(0m, totalBalanceRemaining - currentTermBalance);

                response.Data = new StudentComprehensiveBalanceDto
                {
                    StudentProfileId = studentProfileId,
                    CurrentTermId = currentTermId,
                    PreviousArrears = previousArrears,
                    CurrentTermFee = currentTermExpectedFee,
                    CurrentTermPaid = currentTermPaid,
                    CurrentTermBalance = currentTermBalance,
                    TotalBalanceDue = totalBalanceRemaining
                };

                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Comprehensive student balance retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comprehensive balance for student {StudentId}", studentProfileId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion
    
    }

}