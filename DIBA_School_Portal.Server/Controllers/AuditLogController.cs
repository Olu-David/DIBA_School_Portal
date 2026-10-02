using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Principal")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLog _auditLog;

        public AuditLogController(IAuditLog auditLog)
        {
            _auditLog = auditLog;
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            var logs = await _auditLog.GetLogsAsync(pageNumber, pageSize);
            return Ok(logs);
        }
    }
}