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

        // ---------- Report card — ACADEMIC, no Cashier ----------
        [HttpPost("report-card")]
        [Authorize(Roles = "Student,Parent,Teacher,FormTeacher,Principal,VicePrincipal,Headmaster,Headmistress,SuperAdmin")]
        public IActionResult ReportCard([FromBody] StudentTermReportDto report)
        {
            var pdf = _pdfService.GenerateReportCardPdf(report);
            var safe = (report.StudentName ?? "student").Replace(" ", "_");
            return File(pdf, "application/pdf", $"ReportCard_{safe}_{report.TermName}.pdf");
        }

        // ---------- Payment receipt — FINANCIAL, Cashier YES ----------
        [HttpPost("payment-receipt")]
        [Authorize(Roles = "Student,Parent,Accountant,Cashier,SuperAdmin,Principal")]
        public IActionResult PaymentReceipt([FromBody] StudentPaymentReceiptDto receipt)
        {
            var pdf = _pdfService.GeneratePaymentReceiptPdf(receipt);
            return File(pdf, "application/pdf", $"Receipt_{receipt.ReferenceNumber}.pdf");
        }

        // ---------- School results — ACADEMIC, no Cashier ----------
        [HttpPost("school-results")]
        [Authorize(Roles = "SuperAdmin,Principal,VicePrincipal,Headmaster,Headmistress,SchoolAdmin")]
        public IActionResult SchoolResults([FromBody] SchoolResultsPdfDto report)
        {
            var pdf = _pdfService.GenerateSchoolResultsPdf(report);
            var safe = (report.TermName ?? "Term").Replace(" ", "_");
            return File(pdf, "application/pdf", $"SchoolResults_{safe}.pdf");
        }

        // ---------- Class receipts (batch) — FINANCIAL, Cashier YES ----------
        [HttpPost("class-receipts")]
        [Authorize(Roles = "SuperAdmin,Principal,Accountant,Cashier,SchoolAdmin")]
        public IActionResult ClassReceipts([FromBody] ClassReceiptsDto batch)
        {
            if (batch.Receipts == null || batch.Receipts.Count == 0)
                return BadRequest(new { success = false, message = "No receipts to print." });

            var pdf = _pdfService.GenerateClassReceiptsPdf(batch);
            var safe = (batch.ClassName ?? "Class").Replace(" ", "_");
            return File(pdf, "application/pdf", $"Receipts_{safe}_{batch.TermName}.pdf");
        }

        // ---------- Teacher class sheet — ACADEMIC, no Cashier ----------
        [HttpPost("teacher-class-sheet")]
        [Authorize(Roles = "Teacher,FormTeacher,Principal,VicePrincipal,Headmaster,Headmistress,SuperAdmin,SchoolAdmin")]
        public IActionResult TeacherClassSheet([FromBody] TeacherClassSheetDto sheet)
        {
            if (sheet.Students == null || sheet.Students.Count == 0)
                return BadRequest(new { success = false, message = "No students in this class sheet." });

            var pdf = _pdfService.GenerateTeacherClassSheetPdf(sheet);

            var safeClass = (sheet.ClassName ?? "Class").Replace(" ", "_");
            var safeSubject = (sheet.SubjectName ?? "Subject").Replace(" ", "_");

            return File(pdf, "application/pdf", $"ClassSheet_{safeClass}_{safeSubject}.pdf");
        }
    }
}