using System.Text.Json.Serialization;

namespace DIBA_School_Portal.Server.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentStatus
    {
        Pending,
        Successful,
        Failed,
        Refunded
    }
}
