using DIBA_School_Portal.Server.Contracts;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface ITermSubjectResultService
    {
        // Teacher uses this to save/update bulk scores for a class subject
        Task<ServiceResponse<IEnumerable<StudentScoreEntryDto>>> GetStudentsForScoreEntryAsync(Guid classId, Guid subjectId, Guid termId, Guid academicSessionId);

        Task<ServiceResponse<string>> SaveAndComputeSubjectScoresAsync(ClassScoreSubmissionDto model, string currentUserId);

        // Update this line to return StudentTermReportDto instead of IEnumerable<TermSubjectResultDto>
        Task<ServiceResponse<StudentTermReportDto>>? GetAllSubjectResultsByStudentAsync(string studentId, Guid termId, Guid academicSessionId);
    }
}