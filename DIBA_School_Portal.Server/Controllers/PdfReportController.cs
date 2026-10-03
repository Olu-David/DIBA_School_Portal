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

        [HttpPost("report-card")]
        [Authorize(Roles = "Student,Parent,Teacher,FormTeacher,Principal,VicePrincipal,Headmaster,Headmistress,SuperAdmin")]
        public IActionResult ReportCard([FromBody] StudentTermReportDto report)
        {
            var pdf = _pdfService.GenerateReportCardPdf(report);
            return File(pdf, "application/pdf", $"ReportCard_{report.StudentName}_{report.TermName}.pdf");
        }

        [HttpPost("payment-receipt")]
        [Authorize(Roles = "Student,Parent,Accountant,SuperAdmin,Principal")]
        public IActionResult PaymentReceipt([FromBody] StudentPaymentReceiptDto receipt)
        {
            var pdf = _pdfService.GeneratePaymentReceiptPdf(receipt);
            return File(pdf, "application/pdf", $"Receipt_{receipt.ReferenceNumber}.pdf");
        }
    }
}