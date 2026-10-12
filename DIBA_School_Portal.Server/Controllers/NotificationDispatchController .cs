using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,SchoolAdmin,Registrar")]
    public class NotificationDispatchController : ControllerBase
    {
        private readonly INotificationDispatchService _dispatch;

        public NotificationDispatchController(INotificationDispatchService dispatch)
        {
            _dispatch = dispatch;
        }

        // ---- Current user ID from JWT ----
        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? "System";

        // ---- Client IP for audit trail ----
        private string Ip =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // ==========================================================
        // EMAIL
        // ==========================================================
        /// <summary>
        /// Queue an email blast. Recipients are resolved from the
        /// Target field: "All", "Class", "Section", or "List".
        /// Returns immediately — sending happens in the background.
        /// </summary>
        [HttpPost("email")]
        [ProducesResponseType(typeof(ServiceResponse<SendQueuedResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
        {
            var result = await _dispatch.SendEmailAsync(request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // SMS
        // ==========================================================
        /// <summary>
        /// Queue an SMS blast. Same targeting rules as email.
        /// </summary>
        [HttpPost("sms")]
        [ProducesResponseType(typeof(ServiceResponse<SendQueuedResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendSms([FromBody] SendSmsRequest request)
        {
            var result = await _dispatch.SendSmsAsync(request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }
    }
}