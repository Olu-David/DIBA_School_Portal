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

        // ==========================================================
        // SET OUTSTANDING — Accountant + SuperAdmin only
        // ==========================================================
        [HttpPost("student/{studentProfileId:guid}/outstanding")]
        [Authorize(Roles = "SuperAdmin,Accountant")]
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
        // DASHBOARD — read-only for oversight roles
        // ==========================================================
        [HttpGet("dashboard")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Accountant,Cashier,Registrar")]
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
        // ALL PAYMENTS — read-only
        // ==========================================================
        [HttpGet("payments")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Accountant,Cashier,Registrar")]
        public async Task<IActionResult> Payments([FromQuery] FinancePaymentFilterDto filter)
        {
            var result = await _account.GetPaymentsAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // RECORD CASH PAYMENT — Accountant, Cashier, SuperAdmin
        // ==========================================================
        [HttpPost("cash-payment")]
        [Authorize(Roles = "SuperAdmin,Accountant,Cashier")]
        public async Task<IActionResult> RecordCash([FromBody] RecordCashPaymentDto request)
        {
            var result = await _account.RecordCashPaymentAsync(request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // ARREARS — read-only
        // ==========================================================
        [HttpGet("arrears")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Accountant,Cashier,Registrar")]
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
        // MISC INCOME — record — Accountant, Cashier, SuperAdmin
        // ==========================================================
        [HttpPost("misc-income")]
        [Authorize(Roles = "SuperAdmin,Accountant,Cashier")]
        public async Task<IActionResult> RecordMiscIncome([FromBody] RecordMiscIncomeDto request)
        {
            var result = await _account.RecordMiscIncomeAsync(request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // MISC INCOME — list + filter — read-only
        // ==========================================================
        [HttpGet("misc-income")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Accountant,Cashier,Registrar")]
        public async Task<IActionResult> GetMiscIncome([FromQuery] MiscIncomeFilterDto filter)
        {
            var result = await _account.GetMiscIncomeAsync(filter);
            return StatusCode(result.StatusCode, result);
        }
    }
}