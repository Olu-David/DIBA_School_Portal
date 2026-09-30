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
        public async Task<ServiceResponse<IEnumerable<StudentScoreEntryDto>>> GetStudentsForScoreEntryAsync(Guid classId, Guid subjectId, Guid termId, Guid academicSessionId)
        {
            try
            {
                var studentsInClass = await _userManager.Users
                    .Where(u => u.ClassId == classId)
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

                return new ServiceResponse<IEnumerable<StudentScoreEntryDto>>
                {
                    Success = true,
                    Data = scoreEntryList,
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students for score entry.");
                return new ServiceResponse<IEnumerable<StudentScoreEntryDto>> { Success = false, Message = ex.Message, StatusCode = 500 };
            }
        }

        // ==========================================
        // 2. SAVE SCORES, CALCULATE POSITIONS & NOTIFY PRINCIPAL
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
                            Message = "Invalid scores for student. Test scores must be between 0-20 and Exam between 0-60.",
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
                            Remark = remark
                        };
                        await _context.TermSubjectResults.AddAsync(subjectResult);
                    }
                    else
                    {
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

                var classSubjectResults = await _context.TermSubjectResults
                    .Include(tsr => tsr.TermResult)
                    .Where(tsr => tsr.SubjectId == model.SubjectId
                                  && tsr.TermResult.ClassId == model.ClassId
                                  && tsr.TermResult.TermId == model.TermId
                                  && tsr.TermResult.AcademicSessionId == model.AcademicSessionId)
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

                await ComputeOverallTermResultsAsync(model.ClassId, model.TermId, model.AcademicSessionId);

                if (model.IsFinished)
                {
                    await NotifyPrincipalOfSubmissionAsync(teacher, model.ClassId, model.SubjectId, model.TermId, model.AcademicSessionId);
                }

                return new ServiceResponse<string>
                {
                    Success = true,
                    Message = model.IsFinished
                        ? "Scores finalized and submitted to the Principal successfully!"
                        : "Scores saved as draft successfully!",
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
        // 3. GET A STUDENT'S COMPLETE TERM REPORT
        // ==========================================
        public async Task<ServiceResponse<StudentTermReportDto>> GetAllSubjectResultsByStudentAsync(string studentId, Guid termId, Guid academicSessionId)
        {
            try
            {
                var termResult = await _context.TermResults
                    .AsNoTracking()
                    .Include(tr => tr.Student)
                    .Include(tr => tr.Term)
                    .Include(tr => tr.AcademicSession)
                    .Include(tr => tr.Class)
                    .Include(tr => tr.SubjectResults)
                        .ThenInclude(sr => sr.Subject)
                    .FirstOrDefaultAsync(tr => tr.StudentId == studentId
                                               && tr.TermId == termId
                                               && tr.AcademicSessionId == academicSessionId);

                if (termResult == null)
                {
                    return new ServiceResponse<StudentTermReportDto>
                    {
                        Success = false,
                        Message = "No report card found for this student in the specified term and session.",
                        StatusCode = 404
                    };
                }

                var subjectDtos = termResult.SubjectResults.Select(sr => new TermSubjectResultDto
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

                var reportDto = new StudentTermReportDto
                {
                    TermResultId = termResult.Id,
                    StudentId = termResult.StudentId,
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

                return new ServiceResponse<StudentTermReportDto>
                {
                    Success = true,
                    Data = reportDto,
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching student term report.");
                return new ServiceResponse<StudentTermReportDto> { Success = false, Message = ex.Message, StatusCode = 500 };
            }
        }

        // ==========================================
        // 4. PRIVATE COMPUTATION & HELPER METHODS
        // ==========================================
        private async Task ComputeOverallTermResultsAsync(Guid classId, Guid termId, Guid academicSessionId)
        {
            var termResults = await _context.TermResults
                .Include(tr => tr.SubjectResults)
                .Where(tr => tr.ClassId == classId && tr.TermId == termId && tr.AcademicSessionId == academicSessionId)
                .ToListAsync();

            foreach (var tr in termResults)
            {
                if (tr.SubjectResults.Any())
                {
                    tr.AverageScore = tr.SubjectResults.Average(sr => sr.TotalScore);
                }
                else
                {
                    tr.AverageScore = 0;
                }
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
            if (!principals.Any())
            {
                principals = await _userManager.GetUsersInRoleAsync("Admin");
            }

            var principalEmail = principals.FirstOrDefault()?.Email;

            if (!string.IsNullOrEmpty(principalEmail))
            {
                string subject = $"Score Submission Alert: {subjectName} for {className}";

                string message = $"Dear Principal,<br><br>" +
                                 $"Teacher <b>{teacher.FirstName} {teacher.LastName}</b> has successfully completed and submitted the scores for <b>{subjectName}</b> in <b>{className}</b> ({termName}).<br>" +
                                 $"You can now review the results on the administrative portal.<br><br>" +
                                 $"Best regards,<br>DIBA School Portal System";

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