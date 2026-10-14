using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.ClassDto;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DIBA_School_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PdfReportController : ControllerBase
    {
        private readonly IPdfReportService _pdfService;

        public PdfReportController(IPdfReportService pdfService)
        {
            _pdfService = pdfService;
        }

        // ---------- Report card ----------
        [HttpPost("report-card")]
        [Authorize(Roles = "Student,Parent,Teacher,FormTeacher,Principal,VicePrincipal,Headmaster,Headmistress,SuperAdmin")]
        public IActionResult ReportCard([FromBody] StudentTermReportDto report)
        {
            var pdf = _pdfService.GenerateReportCardPdf(report);
            var safe = (report.StudentName ?? "student").Replace(" ", "_");
            return File(pdf, "application/pdf", $"ReportCard_{safe}_{report.TermName}.pdf");
        }

        // ---------- Payment receipt ----------
        [HttpPost("payment-receipt")]
        [Authorize(Roles = "Student,Parent,Accountant,SuperAdmin,Principal")]
        public IActionResult PaymentReceipt([FromBody] StudentPaymentReceiptDto receipt)
        {
            var pdf = _pdfService.GeneratePaymentReceiptPdf(receipt);
            return File(pdf, "application/pdf", $"Receipt_{receipt.ReferenceNumber}.pdf");
        }

        // ---------- School results ----------
        [HttpPost("school-results")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Headmaster,Headmistress,SchoolAdmin")]
        public IActionResult SchoolResults([FromBody] SchoolResultsPdfDto report)
        {
            var pdf = _pdfService.GenerateSchoolResultsPdf(report);
            var safe = (report.TermName ?? "Term").Replace(" ", "_");
            return File(pdf, "application/pdf", $"SchoolResults_{safe}.pdf");
        }

        // ---------- Class receipts (batch) ----------
        [HttpPost("class-receipts")]
        [Authorize(Roles = "SuperAdmin,Principal,Accountant,SchoolAdmin")]
        public IActionResult ClassReceipts([FromBody] ClassReceiptsDto batch)
        {
            if (batch.Receipts == null || batch.Receipts.Count == 0)
                return BadRequest(new { success = false, message = "No receipts to print." });

            var pdf = _pdfService.GenerateClassReceiptsPdf(batch);
            var safe = (batch.ClassName ?? "Class").Replace(" ", "_");
            return File(pdf, "application/pdf", $"Receipts_{safe}_{batch.TermName}.pdf");
        }
    }
}