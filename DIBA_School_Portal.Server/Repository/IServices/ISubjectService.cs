using DIBA_School_Portal.Server.Contracts;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface ISubjectService
    {
        Task<ServiceResponse<SubjectDto>> GetSubjectByIdAsync(Guid id);
        Task<ServiceResponse<IEnumerable<SubjectDto>>> GetAllSubjectsAsync();
        Task<ServiceResponse<string>> CreateSubjectAsync(SubjectDto model, string currentUserId);
        Task<ServiceResponse<string>> UpdateSubjectAsync(SubjectDto model, string currentUserId);
        Task<bool> SoftDeleteSubject(Guid id, string currentUserId);
        Task<bool> RestoreSubject(Guid id, string currentUserId);
    }
}
