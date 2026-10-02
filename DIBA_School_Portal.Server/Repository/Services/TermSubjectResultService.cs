using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Entities;
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

        public TermSubjectResultService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<TermSubjectResultService> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        // ==========================================
        // 1. GET STUDENTS FOR TEACHER SCORE SHEET
        // ==========================================
        public async Task<ServiceResponse<PaginatedViewModel<StudentScoreEntryDto>>> GetStudentsForScoreEntryAsync(
            Guid classId, Guid subjectId, Guid termId, Guid academicSessionId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Max(1, pageSize);
                int skip = (pageNumber - 1) * pageSize;

                int totalCount = await _userManager.Users
                    .Where(u => u.ClassId == classId && _context.StudentSubjects.Any(ss => ss.StudentId.ToString() == u.Id && ss.SubjectId == subjectId && ss.AcademicSessionId == academicSessionId))
                    .CountAsync();

                var studentsInClass = await _userManager.Users
                    .Where(u => u.ClassId == classId &&
                                _context.StudentSubjects.Any(ss => ss.StudentId.ToString() == u.Id && ss.SubjectId == subjectId && ss.AcademicSessionId == academicSessionId))
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var scoreEntryList = new List<StudentScoreEntryDto>();

                foreach (var student in studentsInClass)
                {
                    var termResult = await _context.TermResults
                        .FirstOrDefaultAsync(tr => tr.StudentId == student.Id
                                                   && tr.ClassId == classId
                                                   && tr.TermId == termId
                                                   && tr.AcademicSessionId == academicSessionId);

                    decimal test1 = 0;
                    decimal test2 = 0;
                    decimal exam = 0;

                    if (termResult != null)
                    {
                        var existingSubjectResult = await _context.TermSubjectResults
                            .FirstOrDefaultAsync(tsr => tsr.TermResultId == termResult.Id && tsr.SubjectId == subjectId);

                        if (existingSubjectResult != null)
                        {
                            test1 = existingSubjectResult.Test1Score;
                            test2 = existingSubjectResult.Test2Score;
                            exam = existingSubjectResult.ExamScore;
                        }
                    }

                    scoreEntryList.Add(new StudentScoreEntryDto
                    {
                        StudentId = student.Id,
                        StudentName = $"{student.FirstName} {student.LastName}",
                        Test1Score = test1,
                        Test2Score = test2,
                        ExamScore = exam
                    });
                }

                var pagedData = new PaginatedViewModel<StudentScoreEntryDto>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    Items = scoreEntryList
                };

                return new ServiceResponse<PaginatedViewModel<StudentScoreEntryDto>>
                {
                    Success = true,
                    Data = pagedData,
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students for score entry.");
                return new ServiceResponse<PaginatedViewModel<StudentScoreEntryDto>>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 500
                };
            }
        }

        // ==========================================
        // 2. SAVE SCORES WITH 48H EDIT CHECK
        // ==========================================
        public async Task<ServiceResponse<string>> SaveAndComputeSubjectScoresAsync(ClassScoreSubmissionDto model, string currentUserId)
        {
            try
            {
                var teacher = await _userManager.FindByIdAsync(currentUserId);
                if (teacher == null)
                {
                    return new ServiceResponse<string> { Success = false, Message = "Teacher not found.", StatusCode = 401 };
                }

                foreach (var item in model.StudentScores)
                {
                    if (item.Test1Score < 0 || item.Test1Score > 20 ||
                        item.Test2Score < 0 || item.Test2Score > 20 ||
                        item.ExamScore < 0 || item.ExamScore > 60)
                    {
                        return new ServiceResponse<string>
                        {
                            Success = false,
                            Message = "Invalid scores. Test scores must be between 0-20 and Exam between 0-60.",
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
                            AcademicSessionId = model.AcademicSessionId
                        };
                        await _context.TermResults.AddAsync(termResult);
                        await _context.SaveChangesAsync();
                    }

                    var subjectResult = await _context.TermSubjectResults
                        .FirstOrDefaultAsync(tsr => tsr.TermResultId == termResult.Id && tsr.SubjectId == model.SubjectId);

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
                        var timeElapsed = DateTime.UtcNow - subjectResult.CreatedAt;
                        if (timeElapsed.TotalHours > 48)
                        {
                            return new ServiceResponse<string>
                            {
                                Success = false,
                                Message = $"Score modification window (48 hours) has expired for student ID {item.StudentId}. Please use the admin override update endpoint.",
                                StatusCode = 403
                            };
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
                await RecalculateRankingsAndAveragesAsync(model.ClassId, model.SubjectId, model.TermId, model.AcademicSessionId);

                if (model.IsFinished)
                {
                    await NotifyPrincipalOfSubmissionAsync(teacher, model.ClassId, model.SubjectId, model.TermId, model.AcademicSessionId);
                }

                return new ServiceResponse<string>
                {
                    Success = true,
                    Message = model.IsFinished ? "Scores finalized and submitted successfully!" : "Scores saved as draft successfully!",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving scores.");
                return new ServiceResponse<string> { Success = false, Message = ex.Message, StatusCode = 500 };
            }
        }

        // ==========================================
        // 3. UPDATE INDIVIDUAL RESULT (BYPASSES OR HANDLES 48H RULE)
        // ==========================================
        public async Task<ServiceResponse<string>> UpdateTermSubjectResultAsync(
             Guid subjectResultId, decimal test1, decimal test2, decimal exam, string currentUserId)
        {
            try
            {
                if (test1 < 0 || test1 > 20 || test2 < 0 || test2 > 20 || exam < 0 || exam > 60)
                {
                    return new ServiceResponse<string>
                    {
                        Success = false,
                        Message = "Invalid score ranges. Tests must be 0-20, Exam must be 0-60.",
                        StatusCode = 400
                    };
                }

                // 1. Fetch current user to verify roles directly on the backend
                var currentUser = await _userManager.FindByIdAsync(currentUserId);
                if (currentUser == null)
                {
                    return new ServiceResponse<string>
                    {
                        Success = false,
                        Message = "Current user not found.",
                        StatusCode = 401
                    };
                }

                bool isPrincipalOrAdmin = await _userManager.IsInRoleAsync(currentUser, "Principal") ||
                                          await _userManager.IsInRoleAsync(currentUser, "SuperAdmin") ||
                                          await _userManager.IsInRoleAsync(currentUser, "Admin");

                var subjectResult = await _context.TermSubjectResults
                    .Include(tsr => tsr.TermResult)
                    .FirstOrDefaultAsync(tsr => tsr.Id == subjectResultId);

                if (subjectResult == null)
                {
                    return new ServiceResponse<string>
                    {
                        Success = false,
                        Message = "Subject result record not found.",
                        StatusCode = 404
                    };
                }

                // 2. Enforce 48-hour restriction for non-Principal/SuperAdmin users
                if (!isPrincipalOrAdmin)
                {
                    var timeElapsed = DateTime.UtcNow - subjectResult.CreatedAt;
                    if (timeElapsed.TotalHours > 48)
                    {
                        return new ServiceResponse<string>
                        {
                            Success = false,
                            Message = "The 48-hour editing window has elapsed. Only a Principal or SuperAdmin can update these scores now.",
                            StatusCode = 403
                        };
                    }
                }

                decimal totalScore = test1 + test2 + exam;

                subjectResult.Test1Score = test1;
                subjectResult.Test2Score = test2;
                subjectResult.ExamScore = exam;
                subjectResult.TotalScore = totalScore;
                subjectResult.Grade = CalculateGrade(totalScore);
                subjectResult.Remark = CalculateRemark(totalScore);

                _context.TermSubjectResults.Update(subjectResult);
                await _context.SaveChangesAsync();

                // 3. Recalculate rankings and term averages automatically
                var tr = subjectResult.TermResult;
                await RecalculateRankingsAndAveragesAsync(tr.ClassId, subjectResult.SubjectId, tr.TermId, tr.AcademicSessionId);

                return new ServiceResponse<string>
                {
                    Success = true,
                    Message = "Subject result updated successfully!",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subject result.");
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 500
                };
            }
        }

        // ==========================================
        // 4. GET REPORT CARD (WITH CUMULATIVE SUPPORT)
        // ==========================================
        public async Task<ServiceResponse<StudentReportCardDto>> GetStudentReportCardAsync(string studentId, Guid termId, Guid academicSessionId)
        {
            try
            {
                if (!Guid.TryParse(studentId, out Guid parsedStudentId))
                {
                    return new ServiceResponse<StudentReportCardDto> { Success = false, Message = "Invalid student ID format.", StatusCode = 400 };
                }

                var isEnrolled = await _context.StudentEnrollments
                    .AnyAsync(e => e.StudentProfileId == parsedStudentId && e.TermId == termId && e.AcademicSessionId == academicSessionId && e.IsActive);

                if (!isEnrolled)
                {
                    return new ServiceResponse<StudentReportCardDto> { Success = false, Message = "Student is not officially enrolled for this term/session.", StatusCode = 400 };
                }

                var term = await _context.SchoolTerms.FindAsync(termId);
                bool isThirdTerm = term != null && term.Name.Contains("Third", StringComparison.OrdinalIgnoreCase);

                if (isThirdTerm)
                {
                    var allTermResultsForSession = await _context.TermResults
                        .AsNoTracking()
                        .Include(tr => tr.Student)
                        .Include(tr => tr.Term)
                        .Include(tr => tr.AcademicSession)
                        .Include(tr => tr.Class)
                        .Include(tr => tr.SubjectResults)
                            .ThenInclude(sr => sr.Subject)
                        .Where(tr => tr.StudentId == studentId && tr.AcademicSessionId == academicSessionId)
                        .ToListAsync();

                    var thirdTermResult = allTermResultsForSession.FirstOrDefault(tr => tr.TermId == termId);
                    if (thirdTermResult == null)
                    {
                        return new ServiceResponse<StudentReportCardDto> { Success = false, Message = "Third term report card not computed yet.", StatusCode = 404 };
                    }

                    var cumulativeSubjectScores = allTermResultsForSession
                        .SelectMany(tr => tr.SubjectResults)
                        .GroupBy(sr => sr.SubjectId)
                        .Select(group => new SubjectScoreDto
                        {
                            SubjectId = group.Key,
                            SubjectName = group.First().Subject?.Name ?? string.Empty,
                            TotalScore = Math.Round(group.Average(sr => sr.TotalScore), 2),
                            Grade = group.Last().Grade,
                            Remark = group.Last().Remark
                        }).ToList();

                    var cumulativeAverage = allTermResultsForSession.Any() ? allTermResultsForSession.Average(tr => tr.AverageScore) : 0;

                    var cumulativeReportDto = new StudentReportCardDto
                    {
                        TermResultId = thirdTermResult.Id,
                        StudentId = thirdTermResult.StudentId.ToString(),
                        StudentName = thirdTermResult.Student != null ? $"{thirdTermResult.Student.FirstName} {thirdTermResult.Student.LastName}" : string.Empty,
                        ClassId = thirdTermResult.ClassId,
                        ClassName = thirdTermResult.Class?.Name ?? string.Empty,
                        TermId = thirdTermResult.TermId,
                        TermName = thirdTermResult.Term?.Name ?? string.Empty,
                        AcademicSessionId = thirdTermResult.AcademicSessionId,
                        SessionName = thirdTermResult.AcademicSession?.Name ?? string.Empty,
                        AverageScore = Math.Round(cumulativeAverage, 2),
                        ClassPosition = thirdTermResult.Position,
                        OverallGrade = thirdTermResult.OverallGrade,
                        SubjectResults = cumulativeSubjectScores
                    };

                    return new ServiceResponse<StudentReportCardDto> { Success = true, Data = cumulativeReportDto, StatusCode = 200 };
                }

                var termResult = await _context.TermResults
                    .AsNoTracking()
                    .Include(tr => tr.Student)
                    .Include(tr => tr.Term)
                    .Include(tr => tr.AcademicSession)
                    .Include(tr => tr.Class)
                    .Include(tr => tr.SubjectResults)
                        .ThenInclude(sr => sr.Subject)
                    .FirstOrDefaultAsync(tr => tr.StudentId == studentId && tr.TermId == termId && tr.AcademicSessionId == academicSessionId);

                if (termResult == null)
                {
                    return new ServiceResponse<StudentReportCardDto> { Success = false, Message = "Report card not found.", StatusCode = 404 };
                }

                var subjectDtos = termResult.SubjectResults.Select(sr => new SubjectScoreDto
                {
                    Id = sr.Id,
                    SubjectId = sr.SubjectId,
                    SubjectName = sr.Subject?.Name ?? string.Empty,
                    Test1Score = sr.Test1Score,
                    Test2Score = sr.Test2Score,
                    ExamScore = sr.ExamScore,
                    TotalScore = sr.TotalScore,
                    SubjectPosition = sr.SubjectPosition,
                    Grade = sr.Grade,
                    Remark = sr.Remark
                }).ToList();

                var reportDto = new StudentReportCardDto
                {
                    TermResultId = termResult.Id,
                    StudentId = termResult.StudentId.ToString(),
                    StudentName = termResult.Student != null ? $"{termResult.Student.FirstName} {termResult.Student.LastName}" : string.Empty,
                    ClassId = termResult.ClassId,
                    ClassName = termResult.Class?.Name ?? string.Empty,
                    TermId = termResult.TermId,
                    TermName = termResult.Term?.Name ?? string.Empty,
                    AcademicSessionId = termResult.AcademicSessionId,
                    SessionName = termResult.AcademicSession?.Name ?? string.Empty,
                    AverageScore = termResult.AverageScore,
                    ClassPosition = termResult.Position,
                    OverallGrade = termResult.OverallGrade,
                    SubjectResults = subjectDtos
                };

                return new ServiceResponse<StudentReportCardDto> { Success = true, Data = reportDto, StatusCode = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching report card.");
                return new ServiceResponse<StudentReportCardDto> { Success = false, Message = ex.Message, StatusCode = 500 };
            }
        }

        // ==========================================
        // 5. HELPER COMPUTATION METHODS
        // ==========================================
        private async Task RecalculateRankingsAndAveragesAsync(Guid classId, Guid subjectId, Guid termId, Guid academicSessionId)
        {
            // 1. Recalculate Subject Positions
            var classSubjectResults = await _context.TermSubjectResults
                .Include(tsr => tsr.TermResult)
                .Where(tsr => tsr.SubjectId == subjectId
                              && tsr.TermResult.ClassId == classId
                              && tsr.TermResult.TermId == termId
                              && tsr.TermResult.AcademicSessionId == academicSessionId)
                .OrderByDescending(tsr => tsr.TotalScore)
                .ToListAsync();

            int rank = 1;
            foreach (var result in classSubjectResults)
            {
                result.SubjectPosition = rank;
                _context.TermSubjectResults.Update(result);
                rank++;
            }
            await _context.SaveChangesAsync();

            // 2. Recalculate Overall Term Averages & Class Positions
            var termResults = await _context.TermResults
                .Include(tr => tr.SubjectResults)
                .Where(tr => tr.ClassId == classId && tr.TermId == termId && tr.AcademicSessionId == academicSessionId)
                .ToListAsync();

            foreach (var tr in termResults)
            {
                tr.AverageScore = tr.SubjectResults.Any() ? tr.SubjectResults.Average(sr => sr.TotalScore) : 0;
            }

            var rankedResults = termResults.OrderByDescending(tr => tr.AverageScore).ToList();
            int classRank = 1;
            foreach (var tr in rankedResults)
            {
                tr.Position = classRank;
                _context.TermResults.Update(tr);
                classRank++;
            }

            await _context.SaveChangesAsync();
        }

        private async Task NotifyPrincipalOfSubmissionAsync(ApplicationUser teacher, Guid classId, Guid subjectId, Guid termId, Guid academicSessionId)
        {
            var className = await _context.SchoolClasses.Where(c => c.Id == classId).Select(c => c.Name).FirstOrDefaultAsync() ?? "Class";
            var subjectName = await _context.Subjects.Where(s => s.Id == subjectId).Select(s => s.Name).FirstOrDefaultAsync() ?? "Subject";
            var termName = await _context.SchoolTerms.Where(t => t.Id == termId).Select(t => t.Name).FirstOrDefaultAsync() ?? "Term";

            var principals = await _userManager.GetUsersInRoleAsync("Principal");
            if (!principals.Any()) principals = await _userManager.GetUsersInRoleAsync("Admin");
            var principalEmail = principals.FirstOrDefault()?.Email;

            if (!string.IsNullOrEmpty(principalEmail))
            {
                string subject = $"Score Submission Alert: {subjectName} for {className}";
                string message = $"Dear Principal,<br><br>" +
                                 $"Teacher <b>{teacher.FirstName} {teacher.LastName}</b> has submitted scores for <b>{subjectName}</b> in <b>{className}</b> ({termName}).";
                await _emailService.EmailSendAsync(principalEmail, subject, message);
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
    }
}