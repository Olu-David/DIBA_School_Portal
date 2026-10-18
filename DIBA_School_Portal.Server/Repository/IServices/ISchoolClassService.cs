using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.ClassDto;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface ISchoolClassService
    {
        Task<ServiceResponse<SchoolClassDTO>> GetSchoolClassByIdAsync(Guid id);
        Task<ServiceResponse<IEnumerable<SchoolClassDTO>>> GetAllSchoolClassesAsync();
        Task<ServiceResponse<IEnumerable<SubjectDto>>> GetSubjectsForClassAsync(Guid classId);
        Task<ServiceResponse<string>> CreateSchoolClassAsync(SchoolClassDTO model, string currentUserId);
        Task<ServiceResponse<string>> UpdateSchoolClassAsync(SchoolClassDTO model, string currentUserId);
        Task<bool> SoftDeleteSchoolClass(Guid id, string currentUserId);
        Task<bool> RestoreSchoolClass(Guid id, string currentUserId);
        Task<ServiceResponse<string>> UpdateClassSubjectsAsync(
     Guid classId, List<ClassSubjectAssignmentDto> items, string currentUserId);
        Task<ServiceResponse<IEnumerable<SubjectCheckboxDto>>> GetClassSubjectsWithSelectionAsync(Guid classId);
    }
}
