using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.PromotionDto;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IPromotionService
    {
  
            Task<ServiceResponse<PromotionPreviewDto>> BuildPreviewAsync(Guid? sessionId = null);
            Task<ServiceResponse<PromotionResultDto>> ExecuteAsync(
                Guid sessionId,
                List<StudentStreamOverride> overrides,
                string currentUserId);
            Task<ServiceResponse<IEnumerable<PromotionRecordDto>>> GetHistoryAsync(Guid sessionId);
        }
}


