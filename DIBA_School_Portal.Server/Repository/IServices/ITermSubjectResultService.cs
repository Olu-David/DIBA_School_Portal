
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.ClassDto;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface ITermSubjectResultService
    {
        

            Task<ServiceResponse<List<TeacherAssignmentDto>>> GetMyAssignmentsAsync(
                string teacherUserId, Guid? academicSessionId = null, Guid? termId = null);

            Task<ServiceResponse<PaginatedViewModel<StudentScoreEntryDto>>> GetStudentsForScoreEntryAsync(
                Guid classId, Guid subjectId, Guid termId, Guid academicSessionId,
                string currentUserId, int pageNumber = 1, int pageSize = 50);

            Task<ServiceResponse<string>> SaveAndComputeSubjectScoresAsync(
                ClassScoreSubmissionDto model, string currentUserId);

            Task<ServiceResponse<string>> UpdateTermSubjectResultAsync(
                Guid subjectResultId, decimal test1, decimal test2, decimal exam,
                string currentUserId);

            Task<ServiceResponse<StudentReportCardDto>> GetStudentReportCardAsync(
                string studentId, Guid termId, Guid academicSessionId);

            Task<ServiceResponse<SchoolResultResponseDto>> GetAllSchoolResultsAsync(
                Guid academicSessionId, Guid termId, AcademicSection? sectionFilter = null);
        }
 }
