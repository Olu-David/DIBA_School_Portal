using DIBA_School_Portal.Server.Contracts.StudentDTO;

namespace DIBA_School_Portal.Server.Services
{
    public interface IPdfReportService
    {
        byte[] GenerateReportCardPdf(StudentTermReportDto report);
        byte[] GeneratePaymentReceiptPdf(StudentPaymentReceiptDto receipt); // Added for receipts
    }
}