using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.ClassDto;
using DIBA_School_Portal.Server.Contracts.StudentDTO;

namespace DIBA_School_Portal.Server.Services
{
    public interface IPdfReportService
    {
        public byte[] GenerateReportCardPdf(StudentTermReportDto report);
        public byte[] GeneratePaymentReceiptPdf(StudentPaymentReceiptDto receipt);
        public byte[] GenerateTeacherClassSheetPdf(TeacherClassSheetDto sheet);
        public byte[] GenerateSchoolResultsPdf(SchoolResultsPdfDto report);
        public byte[] GenerateClassReceiptsPdf(ClassReceiptsDto batch);
    }
}