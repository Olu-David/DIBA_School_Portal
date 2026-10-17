namespace DIBA_School_Portal.Server.Configurations
{
    public class FlutterwaveSettings
    {
        public string PublicKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string EncryptionKey { get; set; } = "";
        public string Currency { get; set; } = "NGN";
        public string BaseUrl { get; set; } = "https://api.flutterwave.com/v3";
        public string RedirectUrl { get; set; } = "";
    }
}
