using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using JsonConverter = Newtonsoft.Json.JsonConverter;


namespace DIBA_School_Portal.Server.Enums
{
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentGateway
    {
        Paystack,
        Flutterwave,
        BankTransfer,
        Cash,
        Manual
    }
}
