using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.DropDown;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class SchoolFeeService : ISchoolFeeService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SchoolFeeService> _logger;

        #region Constructor
        public SchoolFeeService(AppDbContext context, ILogger<SchoolFeeService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region GetFeeCreationMetadataAsync
        public async Task<ServiceResponse<FeeCreationMetadataDto>> GetFeeCreationMetadataAsync()
        {
            var response = new ServiceResponse<FeeCreationMetadataDto>();
            try
            {
                var sessions = await _context.AcademicSessions
                    .AsNoTracking()
                    .Include(s => s.Terms)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => new SessionDropdownItemDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        IsCurrentSession = s.IsCurrentSession,
                        Terms = s.Terms.Select(t => new TermDropdownItemDto
                        {
                            Id = t.Id,
                            Name = t.Name,
                            IsCurrentTerm = t.IsCurrentTerm
                        }).ToList()
                    })
                    .ToListAsync();

                var schoolClasses = await _context.SchoolClasses
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted)
                    .Select(c => new SchoolClassDropdownItemDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Section = c.AcademicSection
                    })
                    .ToListAsync();

                var sections = Enum.GetValues(typeof(AcademicSection))
                    .Cast<AcademicSection>()
                    .Select(e => new EnumLookupDto
                    {
                        Id = (int)e,
                        Name = e.ToString()
                    }).ToList();

                var streams = Enum.GetValues(typeof(ClassStream))
                    .Cast<ClassStream>()
                    .Select(e => new EnumLookupDto
                    {
                        Id = (int)e,
                        Name = e.ToString()
                    }).ToList();

                response.Data = new FeeCreationMetadataDto
                {
                    Sessions = sessions,
                    SchoolClasses = schoolClasses,
                    Sections = sections,
                    Streams = streams
                };

                response.Success = true;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching fee creation metadata.");
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region CreateFeeAsync

        public async Task<ServiceResponse<bool>> CreateFeeAsync(FeeDto feeDto)
        {
            var response = new ServiceResponse<bool>();
            try
            {
                if (feeDto == null)
                {
                    response.Success = false;
                    response.Message = "Fee payload cannot be null.";
                    response.StatusCode = 400;
                    return response;
                }

                var termExists = await _context.SchoolTerms
                    .AnyAsync(t => t.Id == feeDto.TermId && t.AcademicSessionId == feeDto.AcademicSessionId);

                if (!termExists)
                {
                    response.Success = false;
                    response.Message = "The selected term does not belong to the specified academic session.";
                    response.StatusCode = 400;
                    return response;
                }

                var classExists = await _context.SchoolClasses
                    .AnyAsync(c => c.Id == feeDto.SchoolClassId && !c.IsDeleted);

                if (!classExists)
                {
                    response.Success = false;
                    response.Message = "The selected school class does not exist.";
                    response.StatusCode = 400;
                    return response;
                }

                var fee = new Fee
                {
                    Id = Guid.NewGuid(),
                    FeeName = feeDto.FeeName.Trim(),
                    Description = feeDto.Description?.Trim() ?? "",
                    Amount = feeDto.Amount,
                    AcademicSessionId = feeDto.AcademicSessionId,
                    TermId = feeDto.TermId,
                    SchoolClassId = feeDto.SchoolClassId,
                    Section = feeDto.Section,
                    ClassStream = feeDto.Stream
                };

                await _context.Fees.AddAsync(fee);

                // Handle Class-Specific Prices if provided in the DTO
                if (feeDto.ClassPrices != null && feeDto.ClassPrices.Any())
                {
                    foreach (var cp in feeDto.ClassPrices)
                    {
                        var classFeePrice = new ClassFeePrice
                        {
                            Id = Guid.NewGuid(),
                            FeeId = fee.Id,
                            SchoolClassId = cp.SchoolClassId,
                            Amount = cp.MinimumAmountToPay // or cp.Amount depending on your DTO property name
                        };

                        await _context.ClassFeePrices.AddAsync(classFeePrice);
                    }
                }

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Fee structure created successfully.";
                response.StatusCode = 200;
                response.Data = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fee structure.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region GetClassesForSubjectAsync
        public async Task<ServiceResponse<IEnumerable<SchoolClassDTO>>> GetClassesForSubjectAsync(Guid subjectId)
        {
            var response = new ServiceResponse<IEnumerable<SchoolClassDTO>>();
            try
            {
                var classes = await _context.ClassSubjects
                    .AsNoTracking()
                    .Where(cs => cs.SubjectId == subjectId)
                    .Include(cs => cs.SchoolClass)
                    .Where(cs => cs.SchoolClass != null && !cs.SchoolClass.IsDeleted) // Ensure class exists and isn't deleted
                    .Select(cs => new SchoolClassDTO
                    {
                        Id = cs.SchoolClass.Id,
                        Name = cs.SchoolClass.Name,
                        AcademicSection = cs.SchoolClass.AcademicSection
                    })
                    .ToListAsync();

                response.Success = true;
                response.Data = classes;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching school classes for subject: {SubjectId}", subjectId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region GetSchoolClassByIdAsync
        public async Task<ServiceResponse<SchoolClassDTO>> GetSchoolClassByIdAsync(Guid id)
        {
            var response = new ServiceResponse<SchoolClassDTO>();
            try
            {
                var schoolClass = await _context.SchoolClasses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (schoolClass == null)
                {
                    response.Success = false;
                    response.Message = "School class not found.";
                    response.StatusCode = 404;
                    return response;
                }

                response.Data = new SchoolClassDTO
                {
                    Id = schoolClass.Id,
                    Name = schoolClass.Name,
                    AcademicSection = schoolClass.AcademicSection
                };
                response.Success = true;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching school class by ID: {Id}", id);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region UpdateSchoolClassAsync
        public async Task<ServiceResponse<string>> UpdateSchoolClassAsync(SchoolClassDTO model, string currentUserId)
        {
            var response = new ServiceResponse<string>();
            try
            {
                var schoolClass = await _context.SchoolClasses.FirstOrDefaultAsync(c => c.Id == model.Id && !c.IsDeleted);
                if (schoolClass == null)
                {
                    response.Success = false;
                    response.Message = "School class not found.";
                    response.StatusCode = 404;
                    return response;
                }

                schoolClass.Name = model.Name.Trim();
                schoolClass.AcademicSection = model.AcademicSection;

                _context.SchoolClasses.Update(schoolClass);
                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "School class updated successfully.";
                response.StatusCode = 200;
                response.Data = schoolClass.Id.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating school class: {Id}", model.Id);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region SoftDeleteSchoolClass
        public async Task<bool> SoftDeleteSchoolClass(Guid id, string currentUserId)
        {
            try
            {
                var schoolClass = await _context.SchoolClasses.FirstOrDefaultAsync(c => c.Id == id);
                if (schoolClass == null) return false;

                schoolClass.IsDeleted = true;

                _context.SchoolClasses.Update(schoolClass);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting school class: {Id}", id);
                return false;
            }
        }
        #endregion

        #region RestoreSchoolClass
        public async Task<bool> RestoreSchoolClass(Guid id, string currentUserId)
        {
            try
            {
                var schoolClass = await _context.SchoolClasses.FirstOrDefaultAsync(c => c.Id == id);
                if (schoolClass == null) return false;

                schoolClass.IsDeleted = false;
                _context.SchoolClasses.Update(schoolClass);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring school class: {Id}", id);
                return false;
            }
        }
        #endregion

        #region GetSubjectsForClassAsync
        public async Task<ServiceResponse<IEnumerable<SubjectDto>>> GetSubjectsForClassAsync(Guid classId)
        {
            var response = new ServiceResponse<IEnumerable<SubjectDto>>();
            try
            {
                // Query through the junction table to get the subjects assigned to this class
                var subjects = await _context.ClassSubjects
                    .Where(cs => cs.SchoolClassId == classId) 
                    .Include(cs => cs.Subject)              
                    .Select(cs => new SubjectDto
                    {
                        Id = cs.Subject.Id,
                        Name = cs.Subject.Name
                    })
                    .AsNoTracking()
                    .ToListAsync();

                response.Success = true;
                response.Data = subjects;
                response.StatusCode = 200;
            }
             catch (Exception ex) // Keep your catch block as it was (catch (Exception ex))
    {
                _logger.LogError(ex, "Error fetching subjects for class: {ClassId}", classId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        
        #endregion

        #region GetClassSubjectsWithSelectionAsync
        public async Task<ServiceResponse<IEnumerable<SubjectCheckboxDto>>> GetClassSubjectsWithSelectionAsync(Guid classId)
        {
            var response = new ServiceResponse<IEnumerable<SubjectCheckboxDto>>();
            try
            {
                var assignedSubjectIds = await _context.ClassSubjects
                    .Where(cs => cs.SchoolClassId == classId)
                    .Select(cs => cs.SubjectId)
                    .ToListAsync();

                var allSubjects = await _context.Subjects
                    .AsNoTracking()
                    .ToListAsync();

                var result = allSubjects.Select(s => new SubjectCheckboxDto
                {
                    SubjectId = s.Id,
                    Name = s.Name,
                    IsAssigned = assignedSubjectIds.Contains(s.Id)
                }).ToList();

                response.Success = true;
                response.Data = result;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching class subjects with selection for class: {ClassId}", classId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region UpdateClassSubjectsAsync
        public async Task<ServiceResponse<string>> UpdateClassSubjectsAsync(Guid classId, List<Guid> subjectIds, string currentUserId)
        {
            var response = new ServiceResponse<string>();
            try
            {
                var existingClassSubjects = await _context.ClassSubjects
                    .Where(cs => cs.SchoolClassId == classId)
                    .ToListAsync();

                _context.ClassSubjects.RemoveRange(existingClassSubjects);

                if (subjectIds != null && subjectIds.Any())
                {
                    var newClassSubjects = subjectIds.Select(subjectId => new ClassSubject
                    {
                        Id = Guid.NewGuid(),
                        SchoolClassId = classId,
                        SubjectId = subjectId
                    });

                    await _context.ClassSubjects.AddRangeAsync(newClassSubjects);
                }

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Class subjects updated successfully.";
                response.StatusCode = 200;
                response.Data = classId.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating class subjects for class: {ClassId}", classId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region GetStudentBillBreakdownAsync
      
        public async Task<ServiceResponse<StudentBillResponseDto>> GetStudentBillBreakdownAsync(Guid studentProfileId, Guid termId)
        {
            var response = new ServiceResponse<StudentBillResponseDto>();
            try
            {
                var enrollment = await _context.StudentEnrollments
                    .Include(e => e.StudentProfile)
                    .Include(e => e.SchoolClass)
                    .Include(e => e.Term)
                    .FirstOrDefaultAsync(e => e.StudentProfileId == studentProfileId && e.TermId == termId);

                if (enrollment == null)
                {
                    response.Success = false;
                    response.Message = "Student enrollment record not found for this term.";
                    response.StatusCode = 404;
                    return response;
                }

                var student = enrollment.StudentProfile;
                var schoolClass = enrollment.SchoolClass;

                // 1. Calculate Previous Term(s) Outstanding Debt (Arrears)
                decimal previousDebt = await CalculatePreviousOutstandingDebtAsync(studentProfileId, enrollment.Term.StartDate);

                // 2. Fetch standard fee prices mapped to this student's class and term
                var classFeePrices = await _context.ClassFeePrices
                    .Include(cfp => cfp.Fee)
                    .Where(cfp => cfp.SchoolClassId == enrollment.SchoolClassId && cfp.Fee.TermId == termId)
                    .AsNoTracking()
                    .ToListAsync();

                // 3. Fetch all books available for this class and term
                var classBooks = await _context.ClassBooks
                    .Where(cb => cb.SchoolClassId == enrollment.SchoolClassId && cb.TermId == termId)
                    .AsNoTracking()
                    .ToListAsync();

                // 4. Fetch successful payments and their receipt items for this term
                var successfulPayments = await _context.StudentPayments
                    .Include(p => p.PaymentItems)
                    .Where(p => p.StudentProfileId == studentProfileId && p.TermId == termId && p.Status == PaymentStatus.Successful)
                    .AsNoTracking()
                    .ToListAsync();

                var feeBreakdowns = new List<FeeItemBreakdownDto>();
                decimal currentTermBilled = 0;

                // Loop through class fees and calculate what is owed vs. what was paid
                foreach (var cfp in classFeePrices)
                {
                    decimal itemAmount = cfp.Amount;
                    currentTermBilled += itemAmount;

                    decimal paidTowardThis = successfulPayments
                        .SelectMany(p => p.PaymentItems)
                        .Where(pi => pi.FeeId == cfp.FeeId)
                        .Sum(pi => pi.AmountAllocated);

                    feeBreakdowns.Add(new FeeItemBreakdownDto
                    {
                        FeeId = cfp.FeeId,
                        FeeName = cfp.Fee.FeeName,
                        Description = cfp.Fee.Description,
                        Amount = itemAmount,
                        MinimumAmountToPay = cfp.MinimumAmountToPay,
                        AmountPaidTowardThis = paidTowardThis,
                        IsFullyPaid = paidTowardThis >= itemAmount
                    });
                }

                // Loop through books and check if they've been paid for
                var bookBreakdowns = new List<BookItemBreakdownDto>();
                foreach (var book in classBooks)
                {
                    currentTermBilled += book.Price;

                    bool isBookPaid = successfulPayments
                        .SelectMany(p => p.PaymentItems)
                        .Any(pi => pi.ClassBookId == book.Id);

                    bookBreakdowns.Add(new BookItemBreakdownDto
                    {
                        ClassBookId = book.Id,
                        Title = book.Title,
                        Description = book.Description,
                        Price = book.Price,
                        IsPurchased = isBookPaid
                    });
                }

                decimal totalBilled = currentTermBilled + previousDebt;
                decimal totalPaidSoFar = successfulPayments.Sum(p => p.AmountPaid);

                // Fixed syntax error here (added the minus sign)
                decimal balanceRemaining = Math.Max(0, totalBilled - totalPaidSoFar);

                // Calculate minimum required amount for unpaid or partially paid items
                decimal totalMinimumRequired = feeBreakdowns
                    .Where(f => !f.IsFullyPaid)
                    .Sum(f => Math.Max(0, f.MinimumAmountToPay - f.AmountPaidTowardThis));

                var paymentHistory = successfulPayments.Select(p => new PaymentHistoryDto
                {
                    PaymentId = p.Id,
                    ReferenceNumber = p.ReferenceNumber,
                    AmountPaid = p.AmountPaid,
                    Status = p.Status,
                    PaymentDate = p.PaymentDate
                }).ToList();

                response.Data = new StudentBillResponseDto
                {
                    StudentProfileId = student.Id,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    ClassName = schoolClass.Name,
                    TermId = termId,
                    AcademicSessionId = enrollment.AcademicSessionId,
                    TotalBilledAmount = totalBilled,
                    TotalPaidSoFar = totalPaidSoFar,
                    TotalBalanceRemaining = balanceRemaining,
                    MinimumAmountRequired = totalMinimumRequired,
                    FeeItems = feeBreakdowns,
                    AvailableBooks = bookBreakdowns,
                    PaymentHistory = paymentHistory
                };

                response.Success = true;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student bill breakdown");
                response.Success = false;
                response.Message = $"Error retrieving student bill: {ex.Message}";
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region Helper: CalculatePreviousOutstandingDebtAsync
        private async Task<decimal> CalculatePreviousOutstandingDebtAsync(Guid studentProfileId, DateTime? currentTermStartDate)
        {
            var pastEnrollments = await _context.StudentEnrollments
                .Include(e => e.Term)
                .Where(e => e.StudentProfileId == studentProfileId && e.Term.StartDate < currentTermStartDate)
                .AsNoTracking()
                .ToListAsync();

            decimal totalPastBilled = 0;
            decimal totalPastPaid = 0;

            foreach (var pastEnrollment in pastEnrollments)
            {
                var pastFees = await _context.ClassFeePrices
                    .Where(cfp => cfp.SchoolClassId == pastEnrollment.SchoolClassId && cfp.Fee.TermId == pastEnrollment.TermId)
                    .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0;

                var pastBooks = await _context.ClassBooks
                    .Where(cb => cb.SchoolClassId == pastEnrollment.SchoolClassId && cb.TermId == pastEnrollment.TermId)
                    .SumAsync(cb => (decimal?)cb.Price) ?? 0;

                totalPastBilled += (pastFees + pastBooks);

                var pastPayments = await _context.StudentPayments
                    .Where(p => p.StudentProfileId == studentProfileId && p.TermId == pastEnrollment.TermId && p.Status == PaymentStatus.Successful)
                    .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

                totalPastPaid += pastPayments;
            }

            return Math.Max(0, totalPastBilled - totalPastPaid);
        }
        #endregion
    }
}