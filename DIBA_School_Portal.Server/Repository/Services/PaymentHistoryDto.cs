using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class PaymentHistoryDto
    {
        public Guid PaymentId { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal AmountPaid { get; set; }
        public PaymentStatus  Status { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}