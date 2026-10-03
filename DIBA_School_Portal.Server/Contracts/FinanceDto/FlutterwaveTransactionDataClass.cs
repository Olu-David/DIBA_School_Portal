namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class FlutterwaveTransactionDataClass
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public long Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("tx_ref")]
        public string TxRef { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
