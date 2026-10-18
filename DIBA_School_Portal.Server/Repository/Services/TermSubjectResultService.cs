using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.ClassDto;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DIBA_School_Portal.Server.Services
{
    public class TermSubjectResultService : ITermSubjectResultService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<TermSubjectResultService> _logger;
        private readonly IAuditLog _auditLog;
        private readonly INotificationLog _notification;
        private readonly IHttpContextAccessor _http;
        private readonly IPdfReportService _pdfService;        // ← add

        private static readonly string[] AdminOverrideRoles =
            { "SuperAdmin", "Principal", "VicePrincipal", "Headmaster", "Headmistress" };

        public TermSubjectResultService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<TermSubjectResultService> logger,
            IAuditLog auditLog,
            INotificationLog notification,
            IHttpContextAccessor http,
            IPdfReportService pdfService)                       // ← add
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
            _auditLog = auditLog;
            _notification = notification;
            _http = http;
            _pdfService = pdfService;                            // ← add
        }
        private string Ip =>
            _http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        // ==========================================================
        // HELPER — Check assignment
        // ==========================================================
        private async Task<(bool authorized, string reason)> IsTeacherAuthorizedForClassSubjectAsync(
            string userId, Guid classId, Guid subjectId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any(r => AdminOverrideRoles.Contains(r)))
                return (true, "Admin");

            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return (false, "You do not have a teacher profile.");

            var inClass = await _context.TeacherClasses
                .AnyAsync(tc => tc.TeacherId == teacher.Id && tc.SchoolClassId == classId);
            if (!inClass)
                return (false, "You are not assigned to this class.");

            var inSubject = await _context.TeacherSubjects
                .AnyAsync(ts => ts.TeacherId == teacher.Id && ts.SubjectId == subjectId);
            if (!inSubject)
                return (false, "You are not assigned to teach this subject.");

            return (true, "Authorized");
        }

        // ==========================================================
        // MY ASSIGNMENTS
        // ==========================================================
        public async Task<ServiceResponse<List<TeacherAssignmentDto>>> GetMyAssignmentsAsync(
            string teacherUserId, Guid? academicSessionId = null, Guid? termId = null)
        {
            var response = new ServiceResponse<List<TeacherAssignmentDto>>();
            try
            {
                Guid? sessionId = academicSessionId;
                Guid? resolvedTermId = termId;

                if (sessionId == null || resolvedTermId == null)
                {
                    var currentSession = await _context.AcademicSessions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.IsCurrentSession);

                    if (currentSession == null)
                    {
                        response.Success = false;
                        response.Message = "No active session.";
                        response.StatusCode = 404;
                        return response;
                    }

                    sessionId = currentSession.Id;

                    if (resolvedTermId == null)
                    {
                        var currentTerm = await _context.SchoolTerms
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.AcademicSessionId == sessionId && t.IsCurrentTerm);

                        if (currentTerm == null)
                        {
                            response.Success = false;
                            response.Message = "No active term.";
                            response.StatusCode = 404;
                            return response;
                        }
                        resolvedTermId = currentTerm.Id;
                    }
                }

                var teacher = await _context.Teachers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.UserId == teacherUserId);

                if (teacher == null)
                {
                    response.Success = false;
                    response.Message = "You do not have a teacher profile.";
                    response.StatusCode = 404;
                    return response;
                }

                var classIds = await _context.TeacherClasses
                    .AsNoTracking()
                    .Where(tc => tc.TeacherId == teacher.Id)
                    .Select(tc => tc.SchoolClassId)
                    .ToListAsync();

                var subjectIds = await _context.TeacherSubjects
                    .AsNoTracking()
                    .Where(ts => ts.TeacherId == teacher.Id)
                    .Select(ts => ts.SubjectId)
                    .ToListAsync();

                if (!classIds.Any() || !subjectIds.Any())
                {
                    response.Success = true;
                    response.Data = new List<TeacherAssignmentDto>();
                    response.StatusCode = 200;
                    return response;
                }

                var classes = await _context.SchoolClasses
                    .AsNoTracking()
                    .Where(c => classIds.Contains(c.Id) && !c.IsDeleted)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                var subjects = await _context.Subjects
                    .AsNoTracking()
                    .Where(s => subjectIds.Contains(s.Id))
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var studentCounts = await _context.Students
                    .AsNoTracking()
                    .Where(s => classIds.Contains(s.SchoolClassId))
                    .GroupBy(s => s.SchoolClassId)
                    .Select(g => new { ClassId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ClassId, x => x.Count);

                var existingResults = await _context.TermSubjectResults
                    .AsNoTracking()
                    .Include(tsr => tsr.TermResult)
                    .Where(tsr => tsr.TeacherId == teacherUserId
                               && tsr.TermResult.TermId == resolvedTermId
                               && tsr.TermResult.AcademicSessionId == sessionId
                               && classIds.Contains(tsr.TermResult.ClassId)
                               && subjectIds.Contains(tsr.SubjectId))
                    .ToListAsync();

                var assignments = new List<TeacherAssignmentDto>();

                foreach (var cls in classes)
                {
                    foreach (var subj in subjects)
                    {
                        var matching = existingResults
                            .Where(r => r.TermResult.ClassId == cls.Id && r.SubjectId == subj.Id)
                            .ToList();

                        var totalStudents = studentCounts.TryGetValue(cls.Id, out var c) ? c : 0;
                        var scoredCount = matching.Count(r => r.TotalScore > 0);
                        var status = "NotStarted";
                        if (matching.Any()) status = "Draft";
                        if (totalStudents > 0 && scoredCount >= totalStudents) status = "Submitted";

                        assignments.Add(new TeacherAssignmentDto
                        {
                            ClassId = cls.Id,
                            ClassName = cls.Name,
                            Section = cls.AcademicSection,
                            SubjectId = subj.Id,
                            SubjectName = subj.Name,
                            SubjectCode = subj.Code,
                            StudentCount = totalStudents,
                            ScoredCount = scoredCount,
                            SubmissionStatus = status,
                        });
                    }
                }

                response.Success = true;
                response.StatusCode = 200;
                response.Data = assignments;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching assignments");
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
                return response;
            }
        }

        // ==========================================================
        // GET SCORE SHEET
        // ==========================================================
        public async Task<ServiceResponse<PaginatedViewModel<StudentScoreEntryDto>>> GetStudentsForScoreEntryAsync(
            Guid classId, Guid subjectId, Guid termId, Guid academicSessionId,
            string currentUserId, int pageNumber = 1, int pageSize = 50)
        {
            var response = new ServiceResponse<PaginatedViewModel<StudentScoreEntryDto>>();
            try
            {
                var (authorized, reason) = await IsTeacherAuthorizedForClassSubjectAsync(
                    currentUserId, classId, subjectId);
                if (!authorized)
                {
                    response.Success = false;
                    response.Message = reason;
                    response.StatusCode = 403;
                    return response;
                }

                var term = await _context.SchoolTerms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == termId && t.AcademicSessionId == academicSessionId);

                if (term == null)
                {
                    response.Success = false;
                    response.Message = "Term not found or wrong session.";
                    response.StatusCode = 400;
                    return response;
                }

                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Max(1, Math.Min(200, pageSize));
                int skip = (pageNumber - 1) * pageSize;

                var studentsQuery = _context.Students
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Where(s => s.SchoolClassId == classId && s.User != null && !s.User.IsDeleted);

                int totalCount = await studentsQuery.CountAsync();

                var studentsInClass = await studentsQuery
                    .OrderBy(s => s.FirstName)
                    .ThenBy(s => s.LastName)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var userIds = studentsInClass.Select(s => s.UserId).ToList();

                var existingTermResults = await _context.TermResults
                    .AsNoTracking()
                    .Include(tr => tr.SubjectResults)
                    .Where(tr => userIds.Contains(tr.StudentId)
                              && tr.ClassId == classId
                              && tr.TermId == termId
                              && tr.AcademicSessionId == academicSessionId)
                    .ToListAsync();

                var entries = studentsInClass.Select(s =>
                {
                    var tr = existingTermResults.FirstOrDefault(x => x.StudentId == s.UserId);
                    var tsr = tr?.SubjectResults.FirstOrDefault(x => x.SubjectId == subjectId);

                    return new StudentScoreEntryDto
                    {
                        StudentId = s.UserId,
                        StudentProfileId = s.Id,
                        AdmissionNumber = s.AdmissionNumber,
                        StudentName = $"{s.FirstName} {s.LastName}",
                        ProfilePictureUrl = s.User?.ProfilePictureUrl,
                        Test1Score = tsr?.Test1Score ?? 0m,
                        Test2Score = tsr?.Test2Score ?? 0m,
                        ExamScore = tsr?.ExamScore ?? 0m,
                        HasExistingScore = tsr != null,
                    };
                }).ToList();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = new PaginatedViewModel<StudentScoreEntryDto>
                {
                    Items = entries,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching score sheet");
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
                return response;
            }
        }

        // ==========================================================
        // SAVE SCORES
        // ==========================================================
        public async Task<ServiceResponse<string>> SaveAndComputeSubjectScoresAsync(
            ClassScoreSubmissionDto model, string currentUserId)
        {
            try
            {
                var (authorized, reason) = await IsTeacherAuthorizedForClassSubjectAsync(
                    currentUserId, model.ClassId, model.SubjectId);
                if (!authorized)
                    return new ServiceResponse<string> { Success = false, Message = reason, StatusCode = 403 };

                var teacher = await _userManager.FindByIdAsync(currentUserId);
                if (teacher == null)
                    return new ServiceResponse<string> { Success = false, Message = "Teacher not found.", StatusCode = 401 };

                var termExists = await _context.SchoolTerms
                    .AnyAsync(t => t.Id == model.TermId && t.AcademicSessionId == model.AcademicSessionId);
                if (!termExists)
                    return new ServiceResponse<string> { Success = false, Message = "Term mismatch.", StatusCode = 400 };

                var userRoles = await _userManager.GetRolesAsync(teacher);
                bool isAdmin = userRoles.Any(r => AdminOverrideRoles.Contains(r));

                foreach (var item in model.StudentScores)
                {
                    if (item.Test1Score < 0 || item.Test1Score > 20 ||
                        item.Test2Score < 0 || item.Test2Score > 20 ||
                        item.ExamScore < 0 || item.ExamScore > 60)
                    {
                        return new ServiceResponse<string>
                        {
                            Success = false,
                            Message = "Invalid scores. Test 0-20, Exam 0-60.",
                            StatusCode = 400
                        };
                    }

                    var studentProfile = await _context.Students
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.UserId == item.StudentId);

                    if (studentProfile == null)
                        return new ServiceResponse<string>
                        {
                            Success = false,
                            Message = $"Student profile not found for user {item.StudentId}.",
                            StatusCode = 404
                        };

                    // Ensure enrollment exists
                    var enrollment = await _context.StudentEnrollments
                        .FirstOrDefaultAsync(e =>
                            e.StudentProfileId == studentProfile.Id &&
                            e.TermId == model.TermId &&
                            e.AcademicSessionId == model.AcademicSessionId);

                    if (enrollment == null)
                    {
                        var billedAmount = await _context.ClassFeePrices
                            .Include(cfp => cfp.Fee)
                            .Where(cfp =>
                                cfp.SchoolClassId == studentProfile.SchoolClassId &&
                                cfp.Fee.TermId == model.TermId &&
                                cfp.Fee.AcademicSessionId == model.AcademicSessionId)
                            .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0m;

                        enrollment = new StudentEnrollment
                        {
                            Id = Guid.NewGuid(),
                            StudentProfileId = studentProfile.Id,
                            AcademicSessionId = model.AcademicSessionId,
                            TermId = model.TermId,
                            SchoolClassId = studentProfile.SchoolClassId,
                            TotalAmountPaid = 0m,
                            OutstandingBalance = billedAmount,
                            IsFullyPaid = false,
                            Status = StudentEnrollmentStatus.Pending,
                            IsActive = false,
                            EnrolledAt = DateTime.UtcNow,
                        };

                        _context.StudentEnrollments.Add(enrollment);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation(
                            "Auto-created enrollment {EnrollmentId} for student {StudentProfileId} (term {TermId}).",
                            enrollment.Id, studentProfile.Id, model.TermId);
                    }

                    // Find or create TermResult
                    var termResult = await _context.TermResults
                        .FirstOrDefaultAsync(tr =>
                            tr.StudentId == item.StudentId &&
                            tr.ClassId == model.ClassId &&
                            tr.TermId == model.TermId &&
                            tr.AcademicSessionId == model.AcademicSessionId);

                    if (termResult == null)
                    {
                        termResult = new TermResult
                        {
                            Id = Guid.NewGuid(),
                            StudentId = item.StudentId,
                            ClassId = model.ClassId,
                            TermId = model.TermId,
                            AcademicSessionId = model.AcademicSessionId,
                            EnrollmentId = enrollment.Id,
                        };
                        await _context.TermResults.AddAsync(termResult);
                        await _context.SaveChangesAsync();
                    }
                    else if (termResult.EnrollmentId == Guid.Empty)
                    {
                        termResult.EnrollmentId = enrollment.Id;
                        _context.TermResults.Update(termResult);
                        await _context.SaveChangesAsync();
                    }

                    // Find or create TermSubjectResult
                    var subjectResult = await _context.TermSubjectResults
                        .FirstOrDefaultAsync(tsr =>
                            tsr.TermResultId == termResult.Id &&
                            tsr.SubjectId == model.SubjectId);

                    decimal totalScore = item.Test1Score + item.Test2Score + item.ExamScore;
                    string grade = CalculateGrade(totalScore);
                    string remark = CalculateRemark(totalScore);

                    if (subjectResult == null)
                    {
                        subjectResult = new TermSubjectResult
                        {
                            Id = Guid.NewGuid(),
                            TermResultId = termResult.Id,
                            SubjectId = model.SubjectId,
                            TeacherId = currentUserId,
                            Test1Score = item.Test1Score,
                            Test2Score = item.Test2Score,
                            ExamScore = item.ExamScore,
                            TotalScore = totalScore,
                            Grade = grade,
                            Remark = remark,
                            IsDraft = !model.IsFinished,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _context.TermSubjectResults.AddAsync(subjectResult);
                    }
                    else
                    {
                        if (!isAdmin)
                        {
                            var elapsed = DateTime.UtcNow - subjectResult.CreatedAt;
                            if (elapsed.TotalHours > 48)
                            {
                                return new ServiceResponse<string>
                                {
                                    Success = false,
                                    Message = $"48-hour window expired for student {item.StudentId}. Use admin override.",
                                    StatusCode = 403
                                };
                            }
                        }

                        subjectResult.Test1Score = item.Test1Score;
                        subjectResult.Test2Score = item.Test2Score;
                        subjectResult.ExamScore = item.ExamScore;
                        subjectResult.TotalScore = totalScore;
                        subjectResult.Grade = grade;
                        subjectResult.Remark = remark;
                        subjectResult.TeacherId = currentUserId;
                        subjectResult.IsDraft = !model.IsFinished;

                        _context.TermSubjectResults.Update(subjectResult);
                    }
                }

                await _context.SaveChangesAsync();

                await RecalculateRankingsAndAveragesAsync(
                    model.ClassId, model.SubjectId, model.TermId, model.AcademicSessionId);

                await _auditLog.LogActivityAsync(
                    currentUserId,
                    model.IsFinished ? "Scores Submitted (Final)" : "Scores Saved (Draft)",
                    $"Teacher {teacher.FirstName} {teacher.LastName} saved {model.StudentScores.Count} score(s) for class {model.ClassId}, subject {model.SubjectId}.",
                    Ip);

                if (model.IsFinished)
                {
                    await NotifyPrincipalOfSubmissionAsync(
                        teacher, model.ClassId, model.SubjectId, model.TermId, model.AcademicSessionId);
                }

                return new ServiceResponse<string>
                {
                    Success = true,
                    Message = model.IsFinished ? "Scores submitted." : "Scores saved.",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving scores");
                await _auditLog.LogActivityAsync(currentUserId, "Scores Save FAILED", ex.Message, Ip);
                return new ServiceResponse<string> { Success = false, Message = ex.Message, StatusCode = 500 };
            }
        }

        // ==========================================================
        // REPORT CARD PDF — fetch + map + generate, one call
        // ==========================================================
        public async Task<ServiceResponse<byte[]>> GenerateReportCardPdfAsync(
            string studentId, Guid termId, Guid academicSessionId)
        {
            var response = new ServiceResponse<byte[]>();
            try
            {
                // 1. Reuse the existing report-card builder
                var card = await GetStudentReportCardAsync(studentId, termId, academicSessionId);
                if (!card.Success || card.Data == null)
                {
                    response.Success = false;
                    response.Message = card.Message ?? "Report card not found.";
                    response.StatusCode = card.StatusCode;
                    return response;
                }

                var dto = card.Data;

                // 2. Map to the PDF DTO
                var pdfDto = new StudentTermReportDto
                {
                    StudentName = dto.StudentName??"",
                    AdmissionNumber = dto.AdmissionNumber,
                    ProfilePictureUrl = dto.ProfilePictureUrl,
                    ClassName = dto.ClassName??"",
                    Stream = dto.Stream,
                    TermName = dto.TermName??"",
                    SessionName = dto.SessionName??"",
                    AverageScore = dto.AverageScore,
                    ClassPosition = dto.ClassPosition,
                    OverallGrade = dto.OverallGrade!,
                    SubjectResults = (dto.SubjectResults ?? new List<SubjectScoreDto>())
                        .Select(s => new TermSubjectResultDto
                        {
                            SubjectName = s.SubjectName??"",
                            Test1Score = s.Test1Score,
                            Test2Score = s.Test2Score,
                            ExamScore = s.ExamScore,
                            TotalScore = s.TotalScore,
                            Grade = s.Grade!,
                            SubjectPosition = s.SubjectPosition,
                            Remark = s.Remark??"",
                        }).ToList(),
                };

                // 3. Generate
                var bytes = _pdfService.GenerateReportCardPdf(pdfDto);

                if (bytes == null || bytes.Length == 0)
                {
                    response.Success = false;
                    response.Message = "PDF service returned no content.";
                    response.StatusCode = 500;
                    return response;
                }

                response.Success = true;
                response.StatusCode = 200;
                response.Data = bytes;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Report card PDF failed for {StudentId}", studentId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
                return response;
            }
        }

        // ==========================================================
        // UPDATE SINGLE RESULT
        // ==========================================================
        public async Task<ServiceResponse<string>> UpdateTermSubjectResultAsync(
            Guid subjectResultId, decimal test1, decimal test2, decimal exam, string currentUserId)
        {
            try
            {
                if (test1 < 0 || test1 > 20 || test2 < 0 || test2 > 20 || exam < 0 || exam > 60)
                    return new ServiceResponse<string> { Success = false, Message = "Invalid score ranges.", StatusCode = 400 };

                var currentUser = await _userManager.FindByIdAsync(currentUserId);
                if (currentUser == null)
                    return new ServiceResponse<string> { Success = false, Message = "User not found.", StatusCode = 401 };

                var userRoles = await _userManager.GetRolesAsync(currentUser);
                bool isAdmin = userRoles.Any(r => AdminOverrideRoles.Contains(r));

                var subjectResult = await _context.TermSubjectResults
                    .Include(tsr => tsr.TermResult)
                    .FirstOrDefaultAsync(tsr => tsr.Id == subjectResultId);

                if (subjectResult == null)
                    return new ServiceResponse<string> { Success = false, Message = "Result not found.", StatusCode = 404 };

                if (!isAdmin)
                {
                    var elapsed = DateTime.UtcNow - subjectResult.CreatedAt;
                    if (elapsed.TotalHours > 48)
                        return new ServiceResponse<string> { Success = false, Message = "48h window expired.", StatusCode = 403 };

                    var (authorized, reason) = await IsTeacherAuthorizedForClassSubjectAsync(
                        currentUserId, subjectResult.TermResult.ClassId, subjectResult.SubjectId);
                    if (!authorized)
                        return new ServiceResponse<string> { Success = false, Message = reason, StatusCode = 403 };
                }

                var oldTotal = subjectResult.TotalScore;
                decimal totalScore = test1 + test2 + exam;

                subjectResult.Test1Score = test1;
                subjectResult.Test2Score = test2;
                subjectResult.ExamScore = exam;
                subjectResult.TotalScore = totalScore;
                subjectResult.Grade = CalculateGrade(totalScore);
                subjectResult.Remark = CalculateRemark(totalScore);

                _context.TermSubjectResults.Update(subjectResult);
                await _context.SaveChangesAsync();

                var tr = subjectResult.TermResult;
                await RecalculateRankingsAndAveragesAsync(
                    tr.ClassId, subjectResult.SubjectId, tr.TermId, tr.AcademicSessionId);

                await _auditLog.LogActivityAsync(
                    currentUserId,
                    isAdmin ? "Result Override (Admin)" : "Result Updated",
                    $"SubjectResult {subjectResultId}: {oldTotal} → {totalScore}.",
                    Ip);

                return new ServiceResponse<string> { Success = true, Message = "Updated.", StatusCode = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating result");
                await _auditLog.LogActivityAsync(currentUserId, "Result Update FAILED", ex.Message, Ip);
                return new ServiceResponse<string> { Success = false, Message = ex.Message, StatusCode = 500 };
            }
        }

        // ==========================================================
        // REPORT CARD
        // ==========================================================
        public async Task<ServiceResponse<StudentReportCardDto>> GetStudentReportCardAsync(
            string studentId, Guid termId, Guid academicSessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(studentId))
                {
                    return new ServiceResponse<StudentReportCardDto>
                    {
                        Success = false,
                        Message = "Student ID is required.",
                        StatusCode = 400
                    };
                }

                var term = await _context.SchoolTerms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == termId
                                           && t.AcademicSessionId == academicSessionId);

                if (term == null)
                {
                    return new ServiceResponse<StudentReportCardDto>
                    {
                        Success = false,
                        Message = "Term not found or does not belong to this session.",
                        StatusCode = 404
                    };
                }

                bool isThirdTerm = term.Name.Contains("Third", StringComparison.OrdinalIgnoreCase);

                return isThirdTerm
                    ? await BuildCumulativeReportCardAsync(studentId, termId, academicSessionId, term)
                    : await BuildSingleTermReportCardAsync(studentId, termId, academicSessionId, term);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching report card for student {StudentId}", studentId);
                return new ServiceResponse<StudentReportCardDto>
                {
                    Success = false,
                    Message = $"An error occurred: {ex.Message}",
                    StatusCode = 500
                };
            }
        }

        private async Task<ServiceResponse<StudentReportCardDto>> BuildSingleTermReportCardAsync(
            string studentId, Guid termId, Guid academicSessionId, SchoolTerm term)
        {
            var termResult = await _context.TermResults
                .AsNoTracking()
                .Include(tr => tr.Student)
                .Include(tr => tr.Term)
                .Include(tr => tr.AcademicSession)
                .Include(tr => tr.SchoolClass)
                .Include(tr => tr.SubjectResults.Where(sr => !sr.IsDraft))
                    .ThenInclude(sr => sr.Subject)
                .FirstOrDefaultAsync(tr =>
                    tr.StudentId == studentId &&
                    tr.TermId == termId &&
                    tr.AcademicSessionId == academicSessionId);

            if (termResult == null)
            {
                return new ServiceResponse<StudentReportCardDto>
                {
                    Success = false,
                    Message = "Report card not found for this term.",
                    StatusCode = 404
                };
            }

            var studentProfile = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == studentId)
                .Select(s => new { s.Stream, s.AdmissionNumber, s.User!.ProfilePictureUrl })
                .FirstOrDefaultAsync();

            var subjectDtos = termResult.SubjectResults
                .OrderBy(sr => sr.Subject != null ? sr.Subject.Name : "")
                .Select(sr => new SubjectScoreDto
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
                })
                .ToList();

            return new ServiceResponse<StudentReportCardDto>
            {
                Success = true,
                StatusCode = 200,
                Data = new StudentReportCardDto
                {
                    TermResultId = termResult.Id,
                    StudentId = studentId,
                    StudentName = termResult.Student != null
                        ? $"{termResult.Student.FirstName} {termResult.Student.LastName}"
                        : "—",
                    AdmissionNumber = studentProfile?.AdmissionNumber,
                    ProfilePictureUrl = studentProfile?.ProfilePictureUrl,
                    ClassId = termResult.ClassId,
                    ClassName = termResult.SchoolClass?.Name ?? "—",
                    Stream = studentProfile?.Stream,
                    TermId = termResult.TermId,
                    TermName = termResult.Term?.Name ?? term.Name,
                    AcademicSessionId = termResult.AcademicSessionId,
                    SessionName = termResult.AcademicSession?.Name ?? "—",
                    AverageScore = termResult.AverageScore,
                    ClassPosition = termResult.Position,
                    OverallGrade = termResult.OverallGrade,
                    SubjectResults = subjectDtos,
                }
            };
        }

        private async Task<ServiceResponse<StudentReportCardDto>> BuildCumulativeReportCardAsync(
            string studentId, Guid termId, Guid academicSessionId, SchoolTerm term)
        {
            var allResults = await _context.TermResults
                .AsNoTracking()
                .Include(tr => tr.Student)
                .Include(tr => tr.Term)
                .Include(tr => tr.AcademicSession)
                .Include(tr => tr.SchoolClass)
                .Include(tr => tr.SubjectResults.Where(sr => !sr.IsDraft))
                    .ThenInclude(sr => sr.Subject)
                .Where(tr => tr.StudentId == studentId
                          && tr.AcademicSessionId == academicSessionId)
                .ToListAsync();

            var thirdTerm = allResults.FirstOrDefault(tr => tr.TermId == termId);
            if (thirdTerm == null)
            {
                return new ServiceResponse<StudentReportCardDto>
                {
                    Success = false,
                    Message = "Third term report card not computed yet.",
                    StatusCode = 404
                };
            }

            var studentProfile = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == studentId)
                .Select(s => new { s.Stream, s.AdmissionNumber, s.User!.ProfilePictureUrl })
                .FirstOrDefaultAsync();

            var cumulativeSubjects = allResults
                .SelectMany(tr => tr.SubjectResults)
                .GroupBy(sr => sr.SubjectId)
                .Select(g =>
                {
                    var latest = g.OrderByDescending(sr => sr.CreatedAt).First();
                    return new SubjectScoreDto
                    {
                        Id = latest.Id,
                        SubjectId = g.Key,
                        SubjectName = latest.Subject?.Name ?? "—",
                        TotalScore = Math.Round(g.Average(sr => sr.TotalScore), 2),
                        SubjectPosition = latest.SubjectPosition,
                        Grade = latest.Grade,
                        Remark = latest.Remark,
                    };
                })
                .OrderBy(s => s.SubjectName)
                .ToList();

            var cumulativeAverage = allResults.Any()
                ? Math.Round(allResults.Average(tr => tr.AverageScore), 2)
                : 0m;

            var student = allResults.FirstOrDefault()?.Student;

            return new ServiceResponse<StudentReportCardDto>
            {
                Success = true,
                StatusCode = 200,
                Data = new StudentReportCardDto
                {
                    TermResultId = thirdTerm.Id,
                    StudentId = studentId,
                    StudentName = student != null
                        ? $"{student.FirstName} {student.LastName}"
                        : "—",
                    AdmissionNumber = studentProfile?.AdmissionNumber,
                    ProfilePictureUrl = studentProfile?.ProfilePictureUrl,
                    ClassId = thirdTerm.ClassId,
                    ClassName = thirdTerm.SchoolClass?.Name ?? "—",
                    Stream = studentProfile?.Stream,
                    TermId = termId,
                    TermName = term.Name,
                    AcademicSessionId = academicSessionId,
                    SessionName = thirdTerm.AcademicSession?.Name ?? "—",
                    AverageScore = cumulativeAverage,
                    ClassPosition = thirdTerm.Position,
                    OverallGrade = thirdTerm.OverallGrade,
                    SubjectResults = cumulativeSubjects,
                }
            };
        }

        // ==========================================================
        // SCHOOL-WIDE RESULTS — live, no publish gate
        // ==========================================================
        public async Task<ServiceResponse<SchoolResultResponseDto>> GetAllSchoolResultsAsync(
            Guid academicSessionId, Guid termId, AcademicSection? sectionFilter = null)
        {
            var response = new ServiceResponse<SchoolResultResponseDto>();
            try
            {
                var session = await _context.AcademicSessions.FindAsync(academicSessionId);
                var term = await _context.SchoolTerms.FirstOrDefaultAsync(t =>
                    t.Id == termId && t.AcademicSessionId == academicSessionId);

                if (session == null || term == null)
                {
                    response.Success = false;
                    response.Message = "Session or term not found, or term doesn't belong to session.";
                    response.StatusCode = 404;
                    return response;
                }

                // Only non-draft subjects appear.
                var termResults = await _context.TermResults
                    .AsNoTracking()
                    .Include(tr => tr.Student)
                    .Include(tr => tr.SchoolClass)
                    .Include(tr => tr.SubjectResults.Where(sr => !sr.IsDraft))
                    .Where(tr => tr.TermId == termId && tr.AcademicSessionId == academicSessionId)
                    .ToListAsync();

                if (!termResults.Any())
                {
                    response.Success = true;
                    response.StatusCode = 200;
                    response.Data = new SchoolResultResponseDto
                    {
                        AcademicSessionId = academicSessionId,
                        AcademicSessionName = session.Name,
                        TermId = termId,
                        TermName = term.Name,
                        GeneratedAt = DateTime.UtcNow,
                        Classes = new List<ClassResultGroupDto>(),
                    };
                    return response;
                }

                var classIds = termResults.Select(tr => tr.ClassId).Distinct().ToList();
                var userIds = termResults.Select(tr => tr.StudentId).Distinct().ToList();

                var classes = await _context.SchoolClasses
                    .AsNoTracking()
                    .Where(c => classIds.Contains(c.Id))
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                var studentProfiles = await _context.Students
                    .AsNoTracking()
                    .Include(sp => sp.User)
                    .Where(sp => userIds.Contains(sp.UserId))
                    .ToDictionaryAsync(sp => sp.UserId, sp => sp);

                var classSubjectCounts = await _context.ClassSubjects
                    .Where(cs => cs.SchoolClassId.HasValue && classIds.Contains(cs.SchoolClassId.Value))
                    .GroupBy(cs => cs.SchoolClassId!.Value)
                    .Select(g => new { ClassId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ClassId, x => x.Count);

                if (sectionFilter.HasValue)
                {
                    classes = classes
                        .Where(c => c.AcademicSection == sectionFilter.Value)
                        .ToList();
                }

                var classGroups = new List<ClassResultGroupDto>();

                foreach (var schoolClass in classes)
                {
                    var classResults = termResults.Where(tr => tr.ClassId == schoolClass.Id).ToList();
                    if (!classResults.Any()) continue;

                    classSubjectCounts.TryGetValue(schoolClass.Id, out var totalSubjects);

                    var students = classResults.Select(tr =>
                    {
                        studentProfiles.TryGetValue(tr.StudentId, out var profile);

                        var submitted = tr.SubjectResults ?? new List<TermSubjectResult>();

                        var average = submitted.Any()
                            ? Math.Round(submitted.Average(sr => sr.TotalScore), 2)
                            : 0m;

                        return new StudentResultSummaryDto
                        {
                            TermResultId = tr.Id,
                            StudentId = tr.StudentId,
                            StudentName = tr.Student != null
                                ? $"{tr.Student.FirstName} {tr.Student.LastName}".Trim()
                                : "Unknown",
                            AdmissionNumber = profile?.AdmissionNumber ?? "N/A",
                            ProfilePictureUrl = profile?.User?.ProfilePictureUrl,
                            Stream = profile?.Stream,
                            AverageScore = average,
                            TotalMarksObtained = submitted.Sum(sr => sr.TotalScore),
                            Position = tr.Position,
                            OverallGrade = tr.OverallGrade,
                            SubjectCount = submitted.Count,
                            SubmittedSubjectCount = submitted.Count,
                            TotalSubjectCount = totalSubjects,
                        };
                    }).ToList();

                    var streamGroups = students
                        .GroupBy(s => s.Stream)
                        .OrderBy(g => g.Key.HasValue ? (int)g.Key.Value : int.MaxValue)
                        .Select(g => new StreamResultGroupDto
                        {
                            Stream = g.Key,
                            StreamName = g.Key?.ToString() ?? "No Stream",
                            StudentCount = g.Count(),
                            StreamAverage = g.Any()
                                ? Math.Round(g.Average(s => s.AverageScore), 2)
                                : 0m,
                            Students = g
                                .OrderBy(s => s.Position)
                                .ThenBy(s => s.StudentName)
                                .ToList(),
                        })
                        .ToList();

                    var studentsWithData = students.Where(s => s.SubjectCount > 0).ToList();

                    var classAvg = studentsWithData.Any()
                        ? Math.Round(studentsWithData.Average(s => s.AverageScore), 2)
                        : 0m;

                    classGroups.Add(new ClassResultGroupDto
                    {
                        ClassId = schoolClass.Id,
                        ClassName = schoolClass.Name,
                        Section = schoolClass.AcademicSection,
                        DisplayOrder = schoolClass.DisplayOrder,
                        StudentCount = students.Count,
                        ClassAverage = classAvg,
                        HighestAverage = studentsWithData.Any() ? studentsWithData.Max(s => s.AverageScore) : 0m,
                        LowestAverage = studentsWithData.Any() ? studentsWithData.Min(s => s.AverageScore) : 0m,

                        CompleteCount = students.Count(s => s.IsComplete),
                        PendingCount = students.Count(s => !s.IsComplete),

                        Streams = streamGroups,
                    });
                }

                var allStudents = classGroups
                    .SelectMany(c => c.Streams)
                    .SelectMany(s => s.Students)
                    .ToList();

                var withData = allStudents.Where(s => s.SubjectCount > 0).ToList();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = new SchoolResultResponseDto
                {
                    AcademicSessionId = academicSessionId,
                    AcademicSessionName = session.Name,
                    TermId = termId,
                    TermName = term.Name,
                    GeneratedAt = DateTime.UtcNow,
                    TotalStudents = allStudents.Count,
                    TotalClasses = classGroups.Count,
                    TotalComplete = allStudents.Count(s => s.IsComplete),
                    TotalPending = allStudents.Count(s => !s.IsComplete),
                    OverallAverage = withData.Any()
                        ? Math.Round(withData.Average(s => s.AverageScore), 2)
                        : 0m,
                    Classes = classGroups,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all school results");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
    
        // ==========================================================
        // CLASS + SUBJECT RESULTS — teacher or admin
        // ==========================================================
        public async Task<ServiceResponse<ClassSubjectResultDto>> GetClassSubjectResultsAsync(
            Guid classId, Guid subjectId, Guid termId, Guid academicSessionId, string currentUserId)
        {
            var response = new ServiceResponse<ClassSubjectResultDto>();
            try
            {
                // ---- 1. Authorization ----
                var (authorized, reason) = await IsTeacherAuthorizedForClassSubjectAsync(
                    currentUserId, classId, subjectId);
                if (!authorized)
                {
                    response.Success = false;
                    response.Message = reason;
                    response.StatusCode = 403;
                    return response;
                }

                // ---- 2. Load class, subject, term ----
                var schoolClass = await _context.SchoolClasses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == classId);
                if (schoolClass == null)
                {
                    response.Success = false;
                    response.Message = "Class not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var subject = await _context.Subjects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == subjectId);
                if (subject == null)
                {
                    response.Success = false;
                    response.Message = "Subject not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var term = await _context.SchoolTerms
                    .AsNoTracking()
                    .Include(t => t.AcademicSession)
                    .FirstOrDefaultAsync(t => t.Id == termId && t.AcademicSessionId == academicSessionId);
                if (term == null)
                {
                    response.Success = false;
                    response.Message = "Term not found or wrong session.";
                    response.StatusCode = 404;
                    return response;
                }

                // ==========================================================
                // 
                // 1. If scores exist → whoever entered them (TermSubjectResults.TeacherId)
                // 2. Otherwise → the currently assigned teacher for this (class, subject)
                // ==========================================================
                string teacherName = "—";

                // 1. Prefer the teacher recorded on existing score rows
                var enteredBy = await _context.TermSubjectResults
                    .AsNoTracking()
                    .Where(tsr => tsr.SubjectId == subjectId
                               && tsr.TermResult.ClassId == classId
                               && tsr.TermResult.TermId == termId
                               && tsr.TermResult.AcademicSessionId == academicSessionId)
                    .Select(tsr => tsr.TeacherId)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(enteredBy))
                {
                    // TeacherId is stored as a string — might be a User.Id
                    var enteringUser = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == enteredBy);

                    if (enteringUser != null)
                    {
                        teacherName = $"{enteringUser.FirstName} {enteringUser.LastName}".Trim();
                    }
                }

                // 2. Fall back to the currently-assigned teacher for (class, subject)
                if (teacherName == "—")
                {
                    var assigned = await (
                        from ts in _context.TeacherSubjects.AsNoTracking()
                        join tc in _context.TeacherClasses.AsNoTracking()
                            on ts.TeacherId equals tc.TeacherId
                        join t in _context.Teachers.AsNoTracking()
                            on ts.TeacherId equals t.Id
                        where ts.SubjectId == subjectId
                           && tc.SchoolClassId == classId
                        select new { t.FirstName, t.LastName })
                        .FirstOrDefaultAsync();

                    if (assigned != null)
                    {
                        teacherName = $"{assigned.FirstName} {assigned.LastName}".Trim();
                    }
                }

                // ---- 3. Load students in the class ----
                var students = await _context.Students
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Where(s => s.SchoolClassId == classId
                             && s.User != null
                             && !s.User.IsDeleted)
                    .OrderBy(s => s.FirstName)
                    .ThenBy(s => s.LastName)
                    .ToListAsync();

                var studentUserIds = students.Select(s => s.UserId).ToList();

                // ---- 4. Load existing subject results ----
                var subjectResults = await _context.TermSubjectResults
                    .AsNoTracking()
                    .Include(tsr => tsr.TermResult)
                    .Where(tsr => tsr.SubjectId == subjectId
                               && tsr.TermResult.ClassId == classId
                               && tsr.TermResult.TermId == termId
                               && tsr.TermResult.AcademicSessionId == academicSessionId
                               && studentUserIds.Contains(tsr.TermResult.StudentId))
                    .ToListAsync();

                var resultByStudent = subjectResults
                    .GroupBy(tsr => tsr.TermResult.StudentId)
                    .ToDictionary(g => g.Key, g => g.First());

                // ---- 5. Build rows ----
                var rows = students.Select(s =>
                {
                    resultByStudent.TryGetValue(s.UserId, out var tsr);

                    return new ClassSubjectResultRowDto
                    {
                        StudentId = s.UserId,
                        AdmissionNumber = s.AdmissionNumber,
                        StudentName = $"{s.FirstName} {s.LastName}".Trim(),
                        ProfilePictureUrl = s.User?.ProfilePictureUrl,
                        Test1Score = tsr?.Test1Score ?? 0m,
                        Test2Score = tsr?.Test2Score ?? 0m,
                        ExamScore = tsr?.ExamScore ?? 0m,
                        TotalScore = tsr?.TotalScore ?? 0m,
                        Grade = tsr?.Grade ?? "",
                        Remark = tsr?.Remark ?? "",
                        SubjectPosition = tsr?.SubjectPosition ?? 0,
                        IsDraft = tsr?.IsDraft ?? false,
                    };
                }).ToList();

                // ---- 6. Stats ----
                var scored = rows.Where(r => r.TotalScore > 0).ToList();

                var classAvg = scored.Any()
                    ? Math.Round(scored.Average(r => r.TotalScore), 2)
                    : 0m;

                var passCount = scored.Count(r => r.TotalScore >= 40);
                var failCount = scored.Count(r => r.TotalScore > 0 && r.TotalScore < 40);

                // ---- 7. Submission status ----
                var submittedResults = subjectResults.Where(tsr => !tsr.IsDraft).ToList();
                string status = "NotStarted";
                if (subjectResults.Any()) status = "Draft";
                if (students.Count > 0 && submittedResults.Count >= students.Count)
                    status = "Submitted";

                // ---- 8. Response ----
                response.Success = true;
                response.StatusCode = 200;
                response.Data = new ClassSubjectResultDto
                {
                    ClassId = schoolClass.Id,
                    ClassName = schoolClass.Name,
                    Section = schoolClass.AcademicSection,

                    SubjectId = subject.Id,
                    SubjectName = subject.Name,
                    SubjectCode = subject.Code,

                    TermId = term.Id,
                    TermName = term.Name,
                    AcademicSessionId = term.AcademicSessionId,
                    AcademicSessionName = term.AcademicSession?.Name ?? "—",

                    TeacherName = teacherName,
                    SubmissionStatus = status,

                    StudentCount = students.Count,
                    ScoredCount = scored.Count,
                    ClassAverage = classAvg,
                    HighestScore = scored.Any() ? scored.Max(r => r.TotalScore) : 0m,
                    LowestScore = scored.Any() ? scored.Min(r => r.TotalScore) : 0m,
                    PassCount = passCount,
                    FailCount = failCount,

                    Students = rows,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching class-subject results for {ClassId}/{SubjectId}",
                    classId, subjectId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
                return response;
            }
        }
        // ==========================================================
        // OUTSTANDING SUBMISSIONS — who hasn't submitted yet
        // ==========================================================
        public async Task<ServiceResponse<List<OutstandingSubmissionDto>>> GetOutstandingSubmissionsAsync(
            Guid academicSessionId, Guid termId, Guid? classId = null)
        {
            var response = new ServiceResponse<List<OutstandingSubmissionDto>>();
            try
            {
                var classesQuery = _context.SchoolClasses
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted);
                if (classId.HasValue)
                    classesQuery = classesQuery.Where(c => c.Id == classId.Value);

                var classes = await classesQuery
                    .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync();

                var classIds = classes.Select(c => c.Id).ToList();
                if (!classIds.Any())
                {
                    response.Success = true;
                    response.StatusCode = 200;
                    response.Data = new List<OutstandingSubmissionDto>();
                    return response;
                }

                // Every subject each class is supposed to teach
                var classSubjects = await _context.ClassSubjects
                    .Where(cs => cs.SchoolClassId.HasValue
                              && cs.SubjectId.HasValue
                              && classIds.Contains(cs.SchoolClassId.Value))
                    .Select(cs => new
                    {
                        ClassId = cs.SchoolClassId!.Value,
                        SubjectId = cs.SubjectId!.Value
                    })
                    .ToListAsync();

                // Submitted subject results for this term/session
                var submitted = await _context.TermSubjectResults
                    .Where(tsr => !tsr.IsDraft
                               && tsr.TermResult.TermId == termId
                               && tsr.TermResult.AcademicSessionId == academicSessionId
                               && classIds.Contains(tsr.TermResult.ClassId))
                    .GroupBy(tsr => new { tsr.TermResult.ClassId, tsr.SubjectId })
                    .Select(g => new { g.Key.ClassId, g.Key.SubjectId, Count = g.Count() })
                    .ToListAsync();

                // Draft rows — work started but not submitted
                var drafts = await _context.TermSubjectResults
                    .Where(tsr => tsr.IsDraft
                               && tsr.TermResult.TermId == termId
                               && tsr.TermResult.AcademicSessionId == academicSessionId
                               && classIds.Contains(tsr.TermResult.ClassId))
                    .GroupBy(tsr => new { tsr.TermResult.ClassId, tsr.SubjectId })
                    .Select(g => new { g.Key.ClassId, g.Key.SubjectId, Count = g.Count() })
                    .ToListAsync();

                var subjectIds = classSubjects.Select(cs => cs.SubjectId).Distinct().ToList();
                var subjectNames = await _context.Subjects
                    .Where(s => subjectIds.Contains(s.Id))
                    .Select(s => new { s.Id, s.Name })
                    .ToDictionaryAsync(s => s.Id, s => s.Name);

                // ==========================================================
                // FIX: Teacher lookup must be (ClassId, SubjectId) → teacher.
                // Not just SubjectId, which collapses all classes to one teacher.
                // ==========================================================
                var teacherAssignments = await (
                    from ts in _context.TeacherSubjects.AsNoTracking()
                    join tc in _context.TeacherClasses.AsNoTracking()
                        on ts.TeacherId equals tc.TeacherId
                    join t in _context.Teachers.AsNoTracking()
                        on ts.TeacherId equals t.Id
                    where subjectIds.Contains(ts.SubjectId)
                       && classIds.Contains(tc.SchoolClassId)
                    select new
                    {
                        ClassId = tc.SchoolClassId,
                        SubjectId = ts.SubjectId,
                        TeacherFirst = t.FirstName,
                        TeacherLast = t.LastName,
                    })
                    .ToListAsync();

                var teacherByClassSubject = teacherAssignments
                    .GroupBy(x => (x.ClassId, x.SubjectId))
                    .ToDictionary(
                        g => g.Key,
                        g => $"{g.First().TeacherFirst} {g.First().TeacherLast}".Trim());

                var studentCounts = await _context.Students
                    .Where(s => classIds.Contains(s.SchoolClassId))
                    .GroupBy(s => s.SchoolClassId)
                    .Select(g => new { ClassId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ClassId, x => x.Count);

                var result = new List<OutstandingSubmissionDto>();

                foreach (var cs in classSubjects)
                {
                    var hit = submitted.FirstOrDefault(s => s.ClassId == cs.ClassId && s.SubjectId == cs.SubjectId);
                    if (hit != null) continue;

                    var draftHit = drafts.FirstOrDefault(s => s.ClassId == cs.ClassId && s.SubjectId == cs.SubjectId);

                    // 👇 Correct teacher for THIS class + subject
                    var teacherName = teacherByClassSubject.TryGetValue(
                        (cs.ClassId, cs.SubjectId), out var tn) ? tn : "—";

                    result.Add(new OutstandingSubmissionDto
                    {
                        ClassId = cs.ClassId,
                        ClassName = classes.First(c => c.Id == cs.ClassId).Name,
                        SubjectId = cs.SubjectId,
                        SubjectName = subjectNames.TryGetValue(cs.SubjectId, out var n) ? n : "—",
                        TeacherName = teacherName,
                        StudentCount = studentCounts.TryGetValue(cs.ClassId, out var sc) ? sc : 0,
                        ScoredCount = draftHit?.Count ?? 0,
                    });
                }

                result = result
                    .OrderBy(r => r.ClassName)
                    .ThenBy(r => r.SubjectName)
                    .ToList();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = result;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching outstanding submissions");
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
                return response;
            }
        }

        // ==========================================================
        // HELPERS
        // ==========================================================
        private async Task RecalculateRankingsAndAveragesAsync(
            Guid classId, Guid subjectId, Guid termId, Guid academicSessionId)
        {
            // 1. Rank subject results for this subject
            var classSubjectResults = await _context.TermSubjectResults
                .Include(tsr => tsr.TermResult)
                .Where(tsr => tsr.SubjectId == subjectId
                              && tsr.TermResult.ClassId == classId
                              && tsr.TermResult.TermId == termId
                              && tsr.TermResult.AcademicSessionId == academicSessionId)
                .OrderByDescending(tsr => tsr.TotalScore)
                .ToListAsync();

            int rank = 1;
            foreach (var r in classSubjectResults)
            {
                r.SubjectPosition = rank;
                _context.TermSubjectResults.Update(r);
                rank++;
            }
            await _context.SaveChangesAsync();

            // 2. Recompute averages from submitted subjects only
            var termResults = await _context.TermResults
                .Include(tr => tr.SubjectResults)
                .Where(tr => tr.ClassId == classId
                          && tr.TermId == termId
                          && tr.AcademicSessionId == academicSessionId)
                .ToListAsync();

            foreach (var tr in termResults)
            {
                var submitted = tr.SubjectResults
                    .Where(sr => !sr.IsDraft)
                    .ToList();

                tr.AverageScore = submitted.Any()
                    ? submitted.Average(sr => sr.TotalScore)
                    : 0;
            }

            // 3. Rank students by their submitted average
            var ranked = termResults
                .OrderByDescending(tr => tr.AverageScore)
                .ThenByDescending(tr => tr.SubjectResults.Count(sr => !sr.IsDraft))
                .ToList();

            int cr = 1;
            foreach (var tr in ranked)
            {
                tr.Position = cr;
                _context.TermResults.Update(tr);
                cr++;
            }

            await _context.SaveChangesAsync();
        }

        private async Task NotifyPrincipalOfSubmissionAsync(
            ApplicationUser teacher,
            Guid classId,
            Guid subjectId,
            Guid termId,
            Guid academicSessionId)
        {
            var className = await _context.SchoolClasses
                .Where(c => c.Id == classId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? "Class";

            var subjectName = await _context.Subjects
                .Where(s => s.Id == subjectId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync() ?? "Subject";

            var termName = await _context.SchoolTerms
                .Where(t => t.Id == termId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync() ?? "Term";

            var title = "Scores submitted";
            var message =
                $"{teacher.FirstName} {teacher.LastName} submitted {subjectName} " +
                $"for {className} ({termName}).";

            var principals = await _userManager.GetUsersInRoleAsync("Principal");
            if (!principals.Any())
                principals = await _userManager.GetUsersInRoleAsync("VicePrincipal");

            foreach (var p in principals)
            {
                await _notification.SendNotificationAsync(
                    p.Id,
                    title,
                    message,
                    category: "Academic");
            }

            await _auditLog.LogActivityAsync(
                teacher.Id,
                "Scores Submitted",
                $"{teacher.FirstName} {teacher.LastName} submitted {subjectName} for {className} ({termName}).",
                Ip);
        }

        private string CalculateGrade(decimal score)
        {
            if (score >= 70) return "A";
            if (score >= 60) return "B";
            if (score >= 50) return "C";
            if (score >= 45) return "D";
            if (score >= 40) return "E";
            return "F";
        }

        private string CalculateRemark(decimal score)
        {
            if (score >= 70) return "Excellent";
            if (score >= 60) return "Very Good";
            if (score >= 50) return "Good";
            if (score >= 45) return "Fair";
            if (score >= 40) return "Pass";
            return "Poor";
        }
    }
}