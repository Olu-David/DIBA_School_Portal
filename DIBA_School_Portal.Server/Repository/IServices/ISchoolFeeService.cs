using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.DropDown;
using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface ISchoolFeeService
    {
        Task<ServiceResponse<FeeCreationMetadataDto>> GetFeeCreationMetadataAsync();
        Task<ServiceResponse<bool>> CreateFeeAsync(FeeDto feeDto);
        Task<ServiceResponse<IEnumerable<SchoolClassDTO>>> GetAllSchoolClassesAsync();
        Task<ServiceResponse<SchoolClassDTO>> GetSchoolClassByIdAsync(Guid id);
        Task<ServiceResponse<string>> UpdateSchoolClassAsync(SchoolClassDTO model, string currentUserId);
        Task<bool> SoftDeleteSchoolClass(Guid id, string currentUserId);
        Task<bool> RestoreSchoolClass(Guid id, string currentUserId);
        Task<ServiceResponse<IEnumerable<SubjectDto>>> GetSubjectsForClassAsync(Guid classId);
        Task<ServiceResponse<IEnumerable<SubjectCheckboxDto>>> GetClassSubjectsWithSelectionAsync(Guid classId);
        Task<ServiceResponse<string>> UpdateClassSubjectsAsync(Guid classId, List<Guid> subjectIds, string currentUserId);
    }
}
