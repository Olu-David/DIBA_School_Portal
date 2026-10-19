
using DIBA_School_Portal.Server.Contracts.ExpenseDtos;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Headmaster,Headmistress,SchoolAdmin,Accountant")]
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _expense;

        public ExpenseController(IExpenseService expense)
        {
            _expense = expense;
        }

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

        private string Ip =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        [HttpGet]
        public async Task<IActionResult> GetExpenses([FromQuery] ExpenseFilterDto filter)
        {
            var r = await _expense.GetExpensesAsync(filter);
            return StatusCode(r.StatusCode, r);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var r = await _expense.GetExpenseByIdAsync(id);
            return StatusCode(r.StatusCode, r);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExpenseDto request)
        {
            var r = await _expense.CreateExpenseAsync(request, CurrentUserId, Ip);
            return StatusCode(r.StatusCode, r);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseDto request)
        {
            var r = await _expense.UpdateExpenseAsync(id, request, CurrentUserId, Ip);
            return StatusCode(r.StatusCode, r);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var r = await _expense.SoftDeleteExpenseAsync(id, CurrentUserId, Ip);
            return StatusCode(r.StatusCode, r);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] Guid? academicSessionId = null,
            [FromQuery] Guid? termId = null)
        {
            var r = await _expense.GetExpenseDashboardAsync(academicSessionId, termId);
            return StatusCode(r.StatusCode, r);
        }
    }
}