using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.StudentDTO;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IStudentService
    {
        // ---------- READ ----------
        Task<ServiceResponse<PaginatedViewModel<StudentListRowDto>>> GetStudentsAsync(
            StudentFilterDto filter);

        Task<ServiceResponse<StudentDto>> GetStudentByIdAsync(Guid id);

        Task<ServiceResponse<StudentDto>> GetStudentByAdmissionNumberAsync(string admissionNumber);

        Task<ServiceResponse<List<StudentListRowDto>>> GetStudentsByClassAsync(
            Guid classId, bool onlyActive = true);

        // ---------- UPDATE ----------
        Task<ServiceResponse<StudentDto>> UpdateStudentAsync(
            Guid id, UpdateStudentDto request, string currentUserId, string ipAddress);

        // ---------- DELETE / RESTORE ----------
        Task<ServiceResponse<bool>> SoftDeleteStudentAsync(
            Guid id, string currentUserId, string ipAddress);

        Task<ServiceResponse<bool>> RestoreStudentAsync(
            Guid id, string currentUserId, string ipAddress);

        Task<ServiceResponse<StudentBillDto>> GetMyBillAsync( string userId,Guid? termId = null, Guid? academicSessionId = null);
    }
}