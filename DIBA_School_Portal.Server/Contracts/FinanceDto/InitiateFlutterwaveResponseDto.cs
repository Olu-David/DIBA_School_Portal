namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class InitiateFlutterwaveResponseDto
    {
        public Guid PaymentId { get; set; }
        public string ReferenceNumber { get; set; } = "";
        public string CheckoutUrl { get; set; } = "";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "NGN";
    }
}
