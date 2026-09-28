using DIBA_School_Portal.Server.Entities;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IAuditLog 
    {
            Task LogActivityAsync(string userId, string action, string details, string ipAddress);

            Task<IEnumerable<AuditLog>> GetLogsAsync(int pageNumber = 1, int pageSize = 50);
        }
    }
