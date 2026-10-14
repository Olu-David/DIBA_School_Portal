using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.Parent_Child;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class ParentService : IParentService
    {
        #region Fields
        private readonly AppDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly ITermSubjectResultService _resultService;
        private readonly ILogger<ParentService> _logger;
        #endregion

        #region Constructor
        public ParentService(
            AppDbContext context,
            IPaymentService paymentService,
            ITermSubjectResultService resultService,
            ILogger<ParentService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _resultService = resultService ?? throw new ArgumentNullException(nameof(resultService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region Get My Children
        public async Task<ServiceResponse<List<ParentChildSummaryDto>>> GetMyChildrenAsync(
            string parentUserId)
        {
            var response = new ServiceResponse<List<ParentChildSummaryDto>>();

            if (string.IsNullOrWhiteSpace(parentUserId))
            {
                response.Success = false;
                response.Message = "Parent user ID is required.";
                response.StatusCode = 400;
                return response;
            }

            try
            {
                // Pull all children linked to this parent
                var links = await _context.ParentStudents
                    .Where(ps => ps.ParentUserId == parentUserId)
                    .Include(ps => ps.StudentProfile)
                        .ThenInclude(sp => sp.User)
                    .Include(ps => ps.StudentProfile)
                        .ThenInclude(sp => sp.SchoolClass)
                    .OrderBy(ps => ps.StudentProfile.SchoolClass.DisplayOrder)
                    .ThenBy(ps => ps.StudentProfile.FirstName)
                    .ToListAsync();

                if (!links.Any())
                {
                    response.Success = true;
                    response.Message = "No children linked to this account.";
                    response.StatusCode = 200;
                    response.Data = new List<ParentChildSummaryDto>();
                    return response;
                }

                var childIds = links.Select(l => l.StudentProfileId).ToList();

                // Latest enrollment per child → for fee & score summary
                var latestEnrollments = await _context.StudentEnrollments
                    .Where(e => childIds.Contains(e.StudentProfileId))
                    .Include(e => e.Term)
                    .Include(e => e.AcademicSession)
                    .GroupBy(e => e.StudentProfileId)
                    .Select(g => g
                        .OrderByDescending(e => e.Term.StartDate)
                        .First())
                    .ToListAsync();

                var enrollmentMap = latestEnrollments
                    .ToDictionary(e => e.StudentProfileId, e => e);

                // Latest term result per child → for average score
                var latestTermResults = await _context.TermResults
                    .Where(tr => childIds.Select(id => id.ToString()).Contains(tr.StudentId))
                    .Include(tr => tr.Term)
                    .OrderByDescending(tr => tr.Term.StartDate)
                    .ToListAsync();

                var termResultMap = latestTermResults
                    .GroupBy(tr => tr.StudentId)
                    .ToDictionary(g => g.Key, g => g.First());

                // Build the summary list
                var children = links.Select(link =>
                {
                    var sp = link.StudentProfile;
                    var childId = sp.Id;

                    enrollmentMap.TryGetValue(childId, out var enrollment);
                    termResultMap.TryGetValue(sp.UserId, out var latestResult);

                    return new ParentChildSummaryDto
                    {
                        StudentProfileId = childId,
                        AdmissionNumber = sp.AdmissionNumber,
                        FullName = $"{sp.FirstName} {sp.LastName}",
                        ClassName = sp.SchoolClass?.Name ?? "—",
                        ProfilePictureUrl = sp.User?.ProfilePictureUrl,
                        Relationship = link.Relationship,
                        IsPrimaryContact = link.IsPrimaryContact,
                        OutstandingBalance = enrollment?.OutstandingBalance ?? 0m,
                        IsFullyPaid = enrollment?.IsFullyPaid ?? false,
                        LatestAverageScore = latestResult?.AverageScore ?? 0m,
                        LatestTermName = latestResult?.Term?.Name,
                    };
                }).ToList();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = children;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching children for parent {ParentUserId}", parentUserId);
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        #region Get Child Dashboard
        public async Task<ServiceResponse<ChildDashboardDto>> GetChildDashboardAsync(
            string parentUserId, Guid studentProfileId)
        {
            var response = new ServiceResponse<ChildDashboardDto>();

            try
            {
                // ---- Ownership check ----
                if (!await IsParentOfStudentAsync(parentUserId, studentProfileId))
                {
                    response.Success = false;
                    response.Message = "You are not authorized to view this student's data.";
                    response.StatusCode = 403;
                    return response;
                }

                // ---- Child basic info ----
                var student = await _context.Students
                    .Include(s => s.User)
                    .Include(s => s.SchoolClass)
                    .FirstOrDefaultAsync(s => s.Id == studentProfileId);

                if (student == null)
                {
                    response.Success = false;
                    response.Message = "Student not found.";
                    response.StatusCode = 404;
                    return response;
                }

                // ---- Link metadata ----
                var link = await _context.ParentStudents
                    .FirstOrDefaultAsync(ps => ps.ParentUserId == parentUserId
                                            && ps.StudentProfileId == studentProfileId);

                // ---- Current session + term ----
                var currentSession = await _context.AcademicSessions
                    .Where(s => s.IsCurrentSession)
                    .FirstOrDefaultAsync();

                var currentTerm = currentSession != null
                    ? await _context.SchoolTerms
                        .Where(t => t.AcademicSessionId == currentSession.Id && t.IsCurrentTerm)
                        .FirstOrDefaultAsync()
                    : null;

                // ---- Enrollments ----
                var enrollments = await _context.StudentEnrollments
                    .Where(e => e.StudentProfileId == studentProfileId)
                    .Include(e => e.AcademicSession)
                    .Include(e => e.Term)
                    .Include(e => e.SchoolClass)
                    .OrderByDescending(e => e.Term.StartDate)
                    .ToListAsync();

                var enrollmentDtos = enrollments.Select(e => new ChildEnrollmentDto
                {
                    EnrollmentId = e.Id,
                    AcademicSessionId = e.AcademicSessionId,
                    SessionName = e.AcademicSession?.Name ?? "—",
                    TermId = e.TermId,
                    TermName = e.Term?.Name ?? "—",
                    SchoolClassId = e.SchoolClassId,
                    ClassName = e.SchoolClass?.Name ?? "—",
                    Status = e.Status.ToString(),
                    TotalAmountPaid = e.TotalAmountPaid,
                    OutstandingBalance = e.OutstandingBalance,
                    IsFullyPaid = e.IsFullyPaid,
                    EnrolledAt = e.EnrolledAt,
                }).ToList();

                // ---- Current-term fee totals ----
                var currentEnrollment = currentTerm != null
                    ? enrollments.FirstOrDefault(e => e.TermId == currentTerm.Id)
                    : enrollments.FirstOrDefault();

                decimal totalBilled = currentEnrollment != null
                    ? currentEnrollment.TotalAmountPaid + currentEnrollment.OutstandingBalance
                    : 0m;

                decimal totalPaid = currentEnrollment?.TotalAmountPaid ?? 0m;
                decimal outstanding = currentEnrollment?.OutstandingBalance ?? 0m;
                bool isFullyPaid = currentEnrollment?.IsFullyPaid ?? false;

                // ---- Latest report card summary ----
                decimal latestAverage = 0m;
                int latestPosition = 0;
                string? latestGrade = null;
                List<SubjectScoreDto> latestSubjects = new();
                Guid? latestTermId = null;
                Guid? latestSessionId = null;

                if (currentSession != null && currentTerm != null)
                {
                    latestTermId = currentTerm.Id;
                    latestSessionId = currentSession.Id;

                    var latestResult = await _context.TermResults
                        .Include(tr => tr.SubjectResults)
                            .ThenInclude(sr => sr.Subject)
                        .FirstOrDefaultAsync(tr =>
                            tr.StudentId == student.UserId &&
                            tr.TermId == currentTerm.Id &&
                            tr.AcademicSessionId == currentSession.Id);

                    if (latestResult != null)
                    {
                        latestAverage = latestResult.AverageScore;
                        latestPosition = latestResult.Position;
                        latestGrade = latestResult.OverallGrade;

                        latestSubjects = latestResult.SubjectResults.Select(sr => new SubjectScoreDto
                        {
                            Id = sr.Id,
                            SubjectId = sr.SubjectId,
                            SubjectName = sr.Subject?.Name ?? "—",
                            Test1Score = sr.Test1Score,
                            Test2Score = sr.Test2Score,
                            ExamScore = sr.ExamScore,
                            TotalScore = sr.TotalScore,
                            SubjectPosition = sr.SubjectPosition,
                            Grade = sr.Grade,
                            Remark = sr.Remark,
                        }).ToList();
                    }
                }

                // ---- Recent payments ----
                var payments = await _context.StudentPayments
                    .Where(p => p.StudentProfileId == studentProfileId
                             && p.Status == PaymentStatus.Successful)
                    .Include(p => p.AcademicSession)
                    .Include(p => p.Term)
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(10)
                    .ToListAsync();

                var paymentDtos = payments.Select(p => new StudentPaymentDto
                {
                    Id = p.Id,
                    StudentProfileId = p.StudentProfileId,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    AmountPaid = p.AmountPaid,
                    ReferenceNumber = p.ReferenceNumber,
                    Gateway = p.Gateway,
                    Status = p.Status,
                    SessionName = p.AcademicSession?.Name ?? "—",
                    TermName = p.Term?.Name ?? "—",
                    PaymentDate = p.PaymentDate,
                    ProofOfPaymentUrl = p.ProofOfPaymentUrl,
                }).ToList();

                // ---- Compose ----
                response.Success = true;
                response.StatusCode = 200;
                response.Data = new ChildDashboardDto
                {
                    Child = new ParentChildSummaryDto
                    {
                        StudentProfileId = student.Id,
                        AdmissionNumber = student.AdmissionNumber,
                        FullName = $"{student.FirstName} {student.LastName}",
                        ClassName = student.SchoolClass?.Name ?? "—",
                        ProfilePictureUrl = student.User?.ProfilePictureUrl,
                        Relationship = link?.Relationship ?? "Guardian",
                        IsPrimaryContact = link?.IsPrimaryContact ?? false,
                        OutstandingBalance = outstanding,
                        IsFullyPaid = isFullyPaid,
                        LatestAverageScore = latestAverage,
                        LatestTermName = currentTerm?.Name,
                    },
                    CurrentSessionId = currentSession?.Id,
                    CurrentSessionName = currentSession?.Name,
                    CurrentTermId = currentTerm?.Id,
                    CurrentTermName = currentTerm?.Name,
                    TotalBilled = totalBilled,
                    TotalPaid = totalPaid,
                    OutstandingBalance = outstanding,
                    IsFullyPaid = isFullyPaid,
                    LatestAverageScore = latestAverage,
                    LatestClassPosition = latestPosition,
                    LatestOverallGrade = latestGrade,
                    LatestSubjects = latestSubjects,
                    RecentPayments = paymentDtos,
                    Enrollments = enrollmentDtos,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching dashboard for child {StudentProfileId} of parent {ParentUserId}",
                    studentProfileId, parentUserId);
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        #region Get Child Enrollments
        public async Task<ServiceResponse<List<ChildEnrollmentDto>>> GetChildEnrollmentsAsync(
            string parentUserId, Guid studentProfileId)
        {
            var response = new ServiceResponse<List<ChildEnrollmentDto>>();

            try
            {
                if (!await IsParentOfStudentAsync(parentUserId, studentProfileId))
                {
                    response.Success = false;
                    response.Message = "You are not authorized to view this student's data.";
                    response.StatusCode = 403;
                    return response;
                }

                var enrollments = await _context.StudentEnrollments
                    .Where(e => e.StudentProfileId == studentProfileId)
                    .Include(e => e.AcademicSession)
                    .Include(e => e.Term)
                    .Include(e => e.SchoolClass)
                    .OrderByDescending(e => e.Term.StartDate)
                    .Select(e => new ChildEnrollmentDto
                    {
                        EnrollmentId = e.Id,
                        AcademicSessionId = e.AcademicSessionId,
                        SessionName = e.AcademicSession != null ? e.AcademicSession.Name : "—",
                        TermId = e.TermId,
                        TermName = e.Term != null ? e.Term.Name : "—",
                        SchoolClassId = e.SchoolClassId,
                        ClassName = e.SchoolClass != null ? e.SchoolClass.Name : "—",
                        Status = e.Status.ToString(),
                        TotalAmountPaid = e.TotalAmountPaid,
                        OutstandingBalance = e.OutstandingBalance,
                        IsFullyPaid = e.IsFullyPaid,
                        EnrolledAt = e.EnrolledAt,
                    })
                    .ToListAsync();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = enrollments;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enrollments for child {StudentProfileId}", studentProfileId);
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        #region Get Child Bill (delegates to PaymentService after ownership check)
        public async Task<ServiceResponse<StudentBillResponseDto>> GetChildBillAsync(
            string parentUserId, Guid studentProfileId, Guid termId)
        {
            if (!await IsParentOfStudentAsync(parentUserId, studentProfileId))
            {
                return new ServiceResponse<StudentBillResponseDto>
                {
                    Success = false,
                    Message = "You are not authorized to view this student's bill.",
                    StatusCode = 403,
                };
            }

            // Reuse the same logic students use — parent already verified as owner
            return await _paymentService.GetStudentBillBreakdownAsync(studentProfileId, termId);
        }
        #endregion

        #region Get Child Payments
        public async Task<ServiceResponse<List<StudentPaymentDto>>> GetChildPaymentsAsync(
            string parentUserId, Guid studentProfileId)
        {
            var response = new ServiceResponse<List<StudentPaymentDto>>();

            try
            {
                if (!await IsParentOfStudentAsync(parentUserId, studentProfileId))
                {
                    response.Success = false;
                    response.Message = "You are not authorized to view this student's payments.";
                    response.StatusCode = 403;
                    return response;
                }

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Id == studentProfileId);

                if (student == null)
                {
                    response.Success = false;
                    response.Message = "Student not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var payments = await _context.StudentPayments
                    .Where(p => p.StudentProfileId == studentProfileId)
                    .Include(p => p.AcademicSession)
                    .Include(p => p.Term)
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new StudentPaymentDto
                    {
                        Id = p.Id,
                        StudentProfileId = p.StudentProfileId,
                        StudentName = student.FirstName + " " + student.LastName,
                        AmountPaid = p.AmountPaid,
                        ReferenceNumber = p.ReferenceNumber,
                        Gateway = p.Gateway,
                        Status = p.Status,
                        SessionName = p.AcademicSession != null ? p.AcademicSession.Name : "—",
                        TermName = p.Term != null ? p.Term.Name : "—",
                        PaymentDate = p.PaymentDate,
                        ProofOfPaymentUrl = p.ProofOfPaymentUrl,
                    })
                    .ToListAsync();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = payments;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payments for child {StudentProfileId}", studentProfileId);
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        #region Get Child Report Card (delegates to ResultService after ownership check)
        public async Task<ServiceResponse<StudentReportCardDto>> GetChildReportCardAsync(
            string parentUserId, Guid studentProfileId, Guid termId, Guid academicSessionId)
        {
            if (!await IsParentOfStudentAsync(parentUserId, studentProfileId))
            {
                return new ServiceResponse<StudentReportCardDto>
                {
                    Success = false,
                    Message = "You are not authorized to view this student's results.",
                    StatusCode = 403,
                };
            }

            // The result service expects the ApplicationUser.Id (string),
            // so resolve the child's UserId first.
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == studentProfileId);

            if (student == null)
            {
                return new ServiceResponse<StudentReportCardDto>
                {
                    Success = false,
                    Message = "Student not found.",
                    StatusCode = 404,
                };
            }

            return await _resultService.GetStudentReportCardAsync(
                student.UserId, termId, academicSessionId);
        }
        #endregion

        #region Helper - Ownership Check
        private async Task<bool> IsParentOfStudentAsync(string parentUserId, Guid studentProfileId)
        {
            if (string.IsNullOrWhiteSpace(parentUserId) || studentProfileId == Guid.Empty)
                return false;

            return await _context.ParentStudents
                .AnyAsync(ps => ps.ParentUserId == parentUserId
                             && ps.StudentProfileId == studentProfileId);
        }
        #endregion
    
    public async Task<ServiceResponse<PaginatedViewModel<ParentListRowDto>>> GetParentsAsync(
    int pageNumber, int pageSize, string? searchTerm)
        {
            var response = new ServiceResponse<PaginatedViewModel<ParentListRowDto>>();
            try
            {
                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Max(1, Math.Min(200, pageSize));

                // Base query — all ApplicationUsers in the Parent role
                var parentRoleId = await _context.Roles
                    .Where(r => r.Name == "Parent")
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(parentRoleId))
                {
                    response.Success = true;
                    response.StatusCode = 200;
                    response.Data = new PaginatedViewModel<ParentListRowDto>
                    {
                        Items = new List<ParentListRowDto>(),
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalCount = 0,
                        TotalPages = 0,
                    };
                    return response;
                }

                var query = from u in _context.Users
                            join ur in _context.UserRoles on u.Id equals ur.UserId
                            where ur.RoleId == parentRoleId && !u.IsDeleted
                            select u;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var s = searchTerm.Trim().ToLower();
                    query = query.Where(u =>
                        u.FirstName!.ToLower().Contains(s) ||
                        u.LastName!.ToLower().Contains(s) ||
                        (u.Email != null && u.Email.ToLower().Contains(s)) ||
                        (u.PhoneNumber != null && u.PhoneNumber.Contains(s)));
                }

                var totalCount = await query.CountAsync();

                var users = await query
                    .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userIds = users.Select(u => u.Id).ToList();

                // Child counts from ParentStudents
                var childCounts = await _context.ParentStudents
                    .Where(ps => userIds.Contains(ps.ParentUserId))
                    .GroupBy(ps => ps.ParentUserId)
                    .Select(g => new { ParentUserId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ParentUserId, x => x.Count);

                var rows = users.Select(u => new ParentListRowDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName ?? "",
                    LastName = u.LastName ?? "",
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    IsActive = u.IsActive,
                    ChildCount = childCounts.TryGetValue(u.Id, out var c) ? c : 0,
                }).ToList();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = new PaginatedViewModel<ParentListRowDto>
                {
                    Items = rows,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalCount > 0
                        ? (int)Math.Ceiling(totalCount / (double)pageSize)
                        : 0,
                };
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
                return response;
            }
        }
    }
}