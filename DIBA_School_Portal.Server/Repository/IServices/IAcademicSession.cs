using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Entities;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IAcademicSession
    {
        Task<ServiceResponse<bool>> CreateAcademicSessionAsync(AcademicSessionDto Session, SchoolTermDto termDto);
        Task<ServiceResponse<string>> UpdateAcademicSessionAsync(AcademicSessionDto Session, SchoolTermDto termDto);
        Task<ServiceResponse<PaginatedViewModel<AcademicSessionDto>>> GetAllSessionsAsync(Guid SessionID, int pageNumber = 1, int pageSize = 10);
        Task<bool> SetCurrentSessionAsync(Guid sessionId, Guid termId);
        Task<AcademicSessionDto?> GetSessionByIdAsync(Guid sessionId);
        
    }
}
