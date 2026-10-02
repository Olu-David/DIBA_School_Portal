using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Accountant,Registrar")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _account;

        public AccountController(IAccountService account)
        {
            _account = account;
        }

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

        private string Ip =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // ==========================================================
        // DASHBOARD
        // ==========================================================
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(
            [FromQuery] Guid academicSessionId,
            [FromQuery] Guid termId)
        {
            if (academicSessionId == Guid.Empty || termId == Guid.Empty)
                return BadRequest(new { message = "academicSessionId and termId are required." });

            var result = await _account.GetDashboardAsync(academicSessionId, termId);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // ALL PAYMENTS (paginated, filterable)
        // ==========================================================
        [HttpGet("payments")]
        public async Task<IActionResult> Payments([FromQuery] FinancePaymentFilterDto filter)
        {
            var result = await _account.GetPaymentsAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // RECORD CASH PAYMENT
        // ==========================================================
        [HttpPost("cash-payment")]
        public async Task<IActionResult> RecordCash([FromBody] RecordCashPaymentDto request)
        {
            var result = await _account.RecordCashPaymentAsync(request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // ARREARS
        // ==========================================================
        [HttpGet("arrears")]
        public async Task<IActionResult> Arrears(
            [FromQuery] Guid academicSessionId,
            [FromQuery] Guid termId,
            [FromQuery] Guid? classId = null)
        {
            if (academicSessionId == Guid.Empty || termId == Guid.Empty)
                return BadRequest(new { message = "academicSessionId and termId are required." });

            var result = await _account.GetArrearsAsync(academicSessionId, termId, classId);
            return StatusCode(result.StatusCode, result);
        }
    }
}