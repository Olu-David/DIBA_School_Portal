namespace DIBA_School_Portal.Server.Database_Entities
{

    public static class KestrelExtensions
    {
        public static ConfigureWebHostBuilder AddKestrelLimits(
            this ConfigureWebHostBuilder webHost,
            IConfiguration configuration)
        {
            var maxBytes = configuration.GetValue<long?>(
                "Kestrel:MaxRequestBodySizeBytes") ?? 2 * 1024 * 1024;

            webHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = maxBytes;
            });

            return webHost;
        }
    }
}
