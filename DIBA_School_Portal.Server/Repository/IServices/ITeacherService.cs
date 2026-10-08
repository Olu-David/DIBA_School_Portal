using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.Teacher;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface ITeacherService
    {
        // ==========================================================
        // READ
        // ==========================================================
        Task<ServiceResponse<PaginatedViewModel<TeacherRowDto>>> GetTeachersAsync(
            TeacherFilterDto filter);

        Task<ServiceResponse<TeacherDto>> GetTeacherByIdAsync(Guid id);

        Task<ServiceResponse<TeacherDto>> GetTeacherByStaffIdAsync(string staffId);

        Task<ServiceResponse<TeacherDto>> GetTeacherByUserIdAsync(string userId);

        // ==========================================================
        // UPDATE
        // ==========================================================
        Task<ServiceResponse<TeacherDto>> UpdateTeacherAsync(
            Guid id, UpdateTeacherDto request, string currentUserId, string ipAddress);

        // ==========================================================
        // SOFT DELETE / RESTORE
        // ==========================================================
        Task<ServiceResponse<bool>> SoftDeleteTeacherAsync(
            Guid id, string currentUserId, string ipAddress);

        Task<ServiceResponse<bool>> RestoreTeacherAsync(
            Guid id, string currentUserId, string ipAddress);

        // ==========================================================
        // SUBJECT ASSIGNMENTS (TeacherSubject)
        // ==========================================================
        Task<ServiceResponse<bool>> AssignSubjectsAsync(
            Guid teacherId, List<Guid> subjectIds, string currentUserId, string ipAddress);

        Task<ServiceResponse<bool>> UnassignSubjectAsync(
            Guid teacherId, Guid subjectId, string currentUserId, string ipAddress);

        // ==========================================================
        // CLASS ASSIGNMENTS (TeacherClass)
        // ==========================================================
        Task<ServiceResponse<bool>> AssignClassesAsync(
            Guid teacherId, List<Guid> classIds, string currentUserId, string ipAddress);

        Task<ServiceResponse<bool>> UnassignClassAsync(
            Guid teacherId, Guid classId, string currentUserId, string ipAddress);
    }
}