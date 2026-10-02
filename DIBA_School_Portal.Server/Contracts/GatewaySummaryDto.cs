using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts
{
    public class GatewaySummaryDto
    {
        public PaymentGateway Gateway { get; set; }
        public string GatewayName => Gateway.ToString();
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
    }
}
