namespace DIBA_School_Portal.Server.Contracts
{
    public class CashPaymentItemDto
    {
        public Guid? FeeId { get; set; }
        public Guid? ClassBookId { get; set; }
        public decimal AmountAllocated { get; set; }
    }
}
