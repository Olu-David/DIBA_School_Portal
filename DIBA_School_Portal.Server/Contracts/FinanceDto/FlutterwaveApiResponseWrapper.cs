namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class FlutterwaveApiResponseWrapper
    {
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public FlutterwaveTransactionDataClass? Data { get; set; }
    }
}