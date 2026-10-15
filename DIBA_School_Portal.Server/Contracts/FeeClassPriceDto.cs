namespace DIBA_School_Portal.Server.Contracts
{
    public class FeeClassPriceDto
    {
        public Guid SchoolClassId { get; set; }
        public string ClassName { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal MinimumAmountToPay { get; set; }
    }
}