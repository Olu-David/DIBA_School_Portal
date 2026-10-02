using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Accountant")]
    public class PaymentLogController : ControllerBase
    {
        private readonly IPaymentLog _paymentLog;

        public PaymentLogController(IPaymentLog paymentLog)
        {
            _paymentLog = paymentLog;
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent([FromQuery] int count = 50)
        {
            var logs = await _paymentLog.GetRecentPaymentsAsync(count);
            return Ok(logs);
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetByStudent(string studentId)
        {
            var logs = await _paymentLog.GetStudentPaymentsAsync(studentId);
            return Ok(logs);
        }
    }
}