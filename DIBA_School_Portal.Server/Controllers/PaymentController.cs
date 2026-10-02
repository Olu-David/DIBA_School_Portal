using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // BILL BREAKDOWN — student, parent, or finance staff
        [HttpGet("bill/{studentProfileId:guid}/term/{termId:guid}")]
        [Authorize(Roles = "Student,Parent,Accountant,SuperAdmin,Principal,VicePrincipal,Registrar")]
        public async Task<IActionResult> GetBill(Guid studentProfileId, Guid termId)
        {
            var result = await _paymentService.GetStudentBillBreakdownAsync(studentProfileId, termId);
            return StatusCode(result.StatusCode, result);
        }

        // RECORD PAYMENT — Accountant initiates, or Student/Parent self-initiate
        [HttpPost("record")]
        [Authorize(Roles = "Accountant,SuperAdmin,Student,Parent")]
        public async Task<IActionResult> RecordPayment([FromBody] CreatePaymentDto dto)
        {
            var result = await _paymentService.RecordPaymentAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        // VERIFY FLUTTERWAVE — only Accountant or SuperAdmin (webhook-style)
        [HttpPost("verify-flutterwave")]
        [Authorize(Roles = "Accountant,SuperAdmin")]
        public async Task<IActionResult> VerifyFlutterwave([FromQuery] string transactionId, [FromQuery] string referenceNumber)
        {
            var result = await _paymentService.VerifyAndCompleteFlutterwavePaymentAsync(transactionId, referenceNumber);
            return StatusCode(result.StatusCode, result);
        }

        // UPDATE STATUS — Accountant, SuperAdmin, Principal
        [HttpPut("status/{paymentId:guid}")]
        [Authorize(Roles = "Accountant,SuperAdmin,Principal")]
        public async Task<IActionResult> UpdateStatus(Guid paymentId, [FromQuery] PaymentStatus status)
        {
            var result = await _paymentService.UpdatePaymentStatusAsync(paymentId, status);
            return StatusCode(result.StatusCode, result);
        }

        // GET PAYMENTS BY STUDENT
        [HttpGet("student/{studentProfileId:guid}")]
        [Authorize(Roles = "Student,Parent,Accountant,SuperAdmin,Principal,VicePrincipal,Registrar")]
        public async Task<IActionResult> GetByStudent(Guid studentProfileId)
        {
            var result = await _paymentService.GetPaymentsByStudentAsync(studentProfileId);
            return StatusCode(result.StatusCode, result);
        }

        // GET PAYMENT BY ID
        [HttpGet("{paymentId:guid}")]
        [Authorize(Roles = "Student,Parent,Accountant,SuperAdmin,Principal,VicePrincipal,Registrar")]
        public async Task<IActionResult> GetById(Guid paymentId)
        {
            var result = await _paymentService.GetPaymentByIdAsync(paymentId);
            return StatusCode(result.StatusCode, result);
        }

        // GET PAYMENT BY REFERENCE
        [HttpGet("reference/{referenceNumber}")]
        [Authorize(Roles = "Accountant,SuperAdmin,Principal")]
        public async Task<IActionResult> GetByReference(string referenceNumber)
        {
            var result = await _paymentService.GetPaymentByReferenceAsync(referenceNumber);
            return StatusCode(result.StatusCode, result);
        }
    }
}