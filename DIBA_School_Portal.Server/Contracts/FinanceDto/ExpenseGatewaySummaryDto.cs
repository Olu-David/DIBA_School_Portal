using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class ExpenseGatewaySummaryDto
    {

        public PaymentGateway Gateway { get; set; }
        public string GatewayName { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public int Count { get; set; }
    }
}
