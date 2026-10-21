using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Repository.IServices;
using DIBA_School_Portal.Server.Repository.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Accountant,Cashier,Registrar")]
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
        [HttpPost("student/{studentProfileId:guid}/outstanding")]
        [Authorize(Roles = "SuperAdmin,Principal,Accountant,Cashier")]
        public async Task<IActionResult> SetOutstanding(
    Guid studentProfileId,
    [FromBody] SetOutstandingDto dto)
        {
            var result = await _account.SetOutstandingAsync(
                studentProfileId, dto.AcademicSessionId, dto.TermId,
                dto.Amount, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }
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

        // ==========================================================
        // MISC INCOME — record cash outside the fee schedule
        // (Cashier added — see decision note above)
        // ==========================================================
        [HttpPost("misc-income")]
        [Authorize(Roles = "SuperAdmin,Principal,Accountant,Cashier,Registrar")]
        public async Task<IActionResult> RecordMiscIncome([FromBody] RecordMiscIncomeDto request)
        {
            var result = await _account.RecordMiscIncomeAsync(request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // MISC INCOME — list + filter
        // ==========================================================
        [HttpGet("misc-income")]
        public async Task<IActionResult> GetMiscIncome([FromQuery] MiscIncomeFilterDto filter)
        {
            var result = await _account.GetMiscIncomeAsync(filter);
            return StatusCode(result.StatusCode, result);
        }
    }
}