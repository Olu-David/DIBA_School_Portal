using DIBA_School_Portal.Server.Contracts;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface ITermSubjectResultService
    {
        
        Task<ServiceResponse<PaginatedViewModel<StudentScoreEntryDto>>> GetStudentsForScoreEntryAsync(Guid classId, Guid subjectId, Guid termId,
              Guid academicSessionId, int pageNumber = 1, int pageSize = 20);
        Task<ServiceResponse<string>> SaveAndComputeSubjectScoresAsync(ClassScoreSubmissionDto model, string currentUserId);

        // Update this line to return StudentTermReportDto instead of IEnumerable<TermSubjectResultDto>
        Task<ServiceResponse<StudentReportCardDto>> GetStudentReportCardAsync(string studentId, Guid termId, Guid academicSessionId);
        Task<ServiceResponse<string>> UpdateTermSubjectResultAsync(Guid subjectResultId, decimal test1, decimal test2, decimal exam, string currentUserId);
    }
}