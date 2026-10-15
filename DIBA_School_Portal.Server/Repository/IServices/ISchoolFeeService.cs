using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.DropDown;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface ISchoolFeeService
    {
        Task<ServiceResponse<FeeCreationMetadataDto>> GetFeeCreationMetadataAsync();
        Task<ServiceResponse<bool>> CreateFeeAsync(FeeDto feeDto);
        Task<ServiceResponse<IEnumerable<FeeListItemDto>>> GetFeesAsync(Guid? sessionId, Guid? termId, bool includeDeleted);
        Task<ServiceResponse<FeeListItemDto>> GetFeeByIdAsync(Guid id);
        Task<ServiceResponse<string>> SoftDeleteFeeAsync(Guid id, string currentUserId, string ip);
        Task<ServiceResponse<string>> RestoreFeeAsync(Guid id, string currentUserId, string ip);
    }
}