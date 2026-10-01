namespace DIBA_School_Portal.Server.Contracts
{
    public class FeeItemBreakdownDto
    {
        public Guid FeeId { get; set; }
        public string? FeeName { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public decimal MinimumAmountToPay { get; set; }
        public decimal AmountPaidTowardThis { get; set; }
        public bool IsFullyPaid { get; set; }
    }
}