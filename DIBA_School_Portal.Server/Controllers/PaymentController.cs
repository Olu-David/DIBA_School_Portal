using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        private string CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

        private string Ip =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // ==========================================================
        // BILL BREAKDOWN — student, parent, or finance staff
        // ==========================================================
        [HttpGet("bill/{studentProfileId:guid}/term/{termId:guid}")]
        [Authorize(Roles = "Student,Parent,Accountant,Cashier,SuperAdmin,Principal,VicePrincipal,Registrar")]
        public async Task<IActionResult> GetBill(Guid studentProfileId, Guid termId)
        {
            var result = await _paymentService.GetStudentBillBreakdownAsync(
                studentProfileId, termId);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // RECORD PAYMENT — Accountant/Cashier initiates, or Student/Parent self-initiate
        // ==========================================================
        [HttpPost("record")]
        [Authorize(Roles = "Accountant,Cashier,SuperAdmin,Student,Parent")]
        public async Task<IActionResult> RecordPayment([FromBody] CreatePaymentDto dto)
        {
            var result = await _paymentService.RecordPaymentAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // FLUTTERWAVE — INITIATE
        // ==========================================================
        [HttpPost("flutterwave/initiate")]
        [Authorize(Roles = "Student,Parent,Accountant,Cashier,SuperAdmin")]
        public async Task<IActionResult> InitiateFlutterwave(
            [FromBody] InitiateFlutterwaveDto request)
        {
            var result = await _paymentService.InitiateFlutterwaveAsync(
                request, CurrentUserId, Ip);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // FLUTTERWAVE — VERIFY (student / parent self-service)
        // ==========================================================
        [HttpPost("flutterwave/verify")]
        [Authorize(Roles = "Student,Parent,Accountant,Cashier,SuperAdmin,Principal")]
        public async Task<IActionResult> VerifyFlutterwaveStudent(
            [FromQuery] string transactionId,
            [FromQuery] string referenceNumber)
        {
            var result = await _paymentService.VerifyAndCompleteFlutterwavePaymentAsync(
                transactionId, referenceNumber);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // FLUTTERWAVE — VERIFY (legacy)
        // ==========================================================
        [HttpPost("verify-flutterwave")]
        [Authorize(Roles = "Accountant,Cashier,SuperAdmin")]
        public async Task<IActionResult> VerifyFlutterwave(
            [FromQuery] string transactionId,
            [FromQuery] string referenceNumber)
        {
            var result = await _paymentService.VerifyAndCompleteFlutterwavePaymentAsync(
                transactionId, referenceNumber);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // UPDATE STATUS — manual override
        // ⚠️ Cashier included ONLY if cashiers reconcile at counter.
        //    Remove "Cashier" here if Accountant/Principal should own it.
        // ==========================================================
        [HttpPut("status/{paymentId:guid}")]
        [Authorize(Roles = "Accountant,Cashier,SuperAdmin,Principal")]
        public async Task<IActionResult> UpdateStatus(
            Guid paymentId,
            [FromQuery] PaymentStatus status)
        {
            var result = await _paymentService.UpdatePaymentStatusAsync(paymentId, status);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // GET PAYMENTS BY STUDENT
        // ==========================================================
        [HttpGet("student/{studentProfileId:guid}")]
        [Authorize(Roles = "Student,Parent,Accountant,Cashier,SuperAdmin,Principal,VicePrincipal,Registrar")]
        public async Task<IActionResult> GetByStudent(Guid studentProfileId)
        {
            var result = await _paymentService.GetPaymentsByStudentAsync(studentProfileId);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // GET PAYMENT BY ID
        // ==========================================================
        [HttpGet("{paymentId:guid}")]
        [Authorize(Roles = "Student,Parent,Accountant,Cashier,SuperAdmin,Principal,VicePrincipal,Registrar")]
        public async Task<IActionResult> GetById(Guid paymentId)
        {
            var result = await _paymentService.GetPaymentByIdAsync(paymentId);
            return StatusCode(result.StatusCode, result);
        }

        // ==========================================================
        // GET PAYMENT BY REFERENCE
        // ==========================================================
        [HttpGet("reference/{referenceNumber}")]
        [Authorize(Roles = "Accountant,Cashier,SuperAdmin,Principal")]
        public async Task<IActionResult> GetByReference(string referenceNumber)
        {
            var result = await _paymentService.GetPaymentByReferenceAsync(referenceNumber);
            return StatusCode(result.StatusCode, result);
        }
    }
}