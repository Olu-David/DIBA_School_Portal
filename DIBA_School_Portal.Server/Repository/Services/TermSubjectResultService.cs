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

        private static readonly string[] AdminOverrideRoles =
            { "SuperAdmin", "Principal", "VicePrincipal", "Headmaster", "Headmistress" };

        public TermSubjectResultService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<TermSubjectResultService> logger,
            IAuditLog auditLog,
            INotificationLog notification,
            IHttpContextAccessor http)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
            _auditLog = auditLog;
            _notification = notification;
            _http = http;
        }

        private string Ip =>
            _http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        // ==========================================================
        // HELPER — Check assignment
        // ==========================================================
        #region IsTeacherAuthorizedForClassSubjectAsync
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
        #endregion

        // ==========================================================
        // MY ASSIGNMENTS
        // ==========================================================
        #region GetMyAssignmentsAsync
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
        #endregion

        // ==========================================================
        // GET SCORE SHEET
        // ==========================================================
        #region GetStudentsForScoreEntryAsync
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
        #endregion

        // ==========================================================
        // SAVE SCORES
        // ==========================================================
        #region SaveAndComputeSubjectScoresAsync
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

                    var termResult = await _context.TermResults
                        .FirstOrDefaultAsync(tr => tr.StudentId == item.StudentId
                                                && tr.ClassId == model.ClassId
                                                && tr.TermId == model.TermId
                                                && tr.AcademicSessionId == model.AcademicSessionId);

                    if (termResult == null)
                    {
                        termResult = new TermResult
                        {
                            Id = Guid.NewGuid(),
                            StudentId = item.StudentId,
                            ClassId = model.ClassId,
                            TermId = model.TermId,
                            AcademicSessionId = model.AcademicSessionId,
                        };
                        await _context.TermResults.AddAsync(termResult);
                        await _context.SaveChangesAsync();
                    }

                    var subjectResult = await _context.TermSubjectResults
                        .FirstOrDefaultAsync(tsr => tsr.TermResultId == termResult.Id
                                                 && tsr.SubjectId == model.SubjectId);

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

                    var principals = await _userManager.GetUsersInRoleAsync("Principal");
                    foreach (var p in principals)
                    {
                        await _notification.SendNotificationAsync(
                            p.Id,
                            "Scores Submitted",
                            $"{teacher.FirstName} {teacher.LastName} submitted scores for class {model.ClassId}.",
                            category: "Academic");
                    }
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
        #endregion

        // ==========================================================
        // UPDATE SINGLE RESULT
        // ==========================================================
        #region UpdateTermSubjectResultAsync
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
        #endregion

        // ==========================================================
        // REPORT CARD — paste your existing implementation
        // ==========================================================
        #region GetStudentReportCardAsync
        public async Task<ServiceResponse<StudentReportCardDto>> GetStudentReportCardAsync(
            string studentId, Guid termId, Guid academicSessionId)
        {
            throw new NotImplementedException("Paste your existing implementation here.");
        }
        #endregion

        // ==========================================================
        // SCHOOL-WIDE RESULTS — paste your existing implementation
        // ==========================================================
        #region GetAllSchoolResultsAsync
        public async Task<ServiceResponse<SchoolResultResponseDto>> GetAllSchoolResultsAsync(
            Guid academicSessionId, Guid termId, AcademicSection? sectionFilter = null)
        {
            throw new NotImplementedException("Paste your existing implementation here.");
        }
        #endregion

        // ==========================================================
        // HELPERS
        // ==========================================================
        #region Helpers
        private async Task RecalculateRankingsAndAveragesAsync(
            Guid classId, Guid subjectId, Guid termId, Guid academicSessionId)
        {
            var classSubjectResults = await _context.TermSubjectResults
                .Include(tsr => tsr.TermResult)
                .Where(tsr => tsr.SubjectId == subjectId
                              && tsr.TermResult.ClassId == classId
                              && tsr.TermResult.TermId == termId
                              && tsr.TermResult.AcademicSessionId == academicSessionId)
                .OrderByDescending(tsr => tsr.TotalScore)
                .ToListAsync();

            int rank = 1;
            foreach (var r in classSubjectResults) { r.SubjectPosition = rank; _context.TermSubjectResults.Update(r); rank++; }
            await _context.SaveChangesAsync();

            var termResults = await _context.TermResults
                .Include(tr => tr.SubjectResults)
                .Where(tr => tr.ClassId == classId && tr.TermId == termId && tr.AcademicSessionId == academicSessionId)
                .ToListAsync();

            foreach (var tr in termResults)
                tr.AverageScore = tr.SubjectResults.Any() ? tr.SubjectResults.Average(sr => sr.TotalScore) : 0;

            var ranked = termResults.OrderByDescending(tr => tr.AverageScore).ToList();
            int cr = 1;
            foreach (var tr in ranked) { tr.Position = cr; _context.TermResults.Update(tr); cr++; }
            await _context.SaveChangesAsync();
        }

        private async Task NotifyPrincipalOfSubmissionAsync(
            ApplicationUser teacher, Guid classId, Guid subjectId, Guid termId, Guid academicSessionId)
        {
            var className = await _context.SchoolClasses.Where(c => c.Id == classId).Select(c => c.Name).FirstOrDefaultAsync() ?? "Class";
            var subjectName = await _context.Subjects.Where(s => s.Id == subjectId).Select(s => s.Name).FirstOrDefaultAsync() ?? "Subject";
            var termName = await _context.SchoolTerms.Where(t => t.Id == termId).Select(t => t.Name).FirstOrDefaultAsync() ?? "Term";

            var principals = await _userManager.GetUsersInRoleAsync("Principal");
            if (!principals.Any()) principals = await _userManager.GetUsersInRoleAsync("Admin");
            var email = principals.FirstOrDefault()?.Email;

            if (!string.IsNullOrEmpty(email))
            {
                await _emailService.EmailSendAsync(
                    email,
                    $"Score Submission Alert: {subjectName} for {className}",
                    $"Teacher <b>{teacher.FirstName} {teacher.LastName}</b> submitted scores for <b>{subjectName}</b> in <b>{className}</b> ({termName}).");
            }
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
        #endregion
    }
}