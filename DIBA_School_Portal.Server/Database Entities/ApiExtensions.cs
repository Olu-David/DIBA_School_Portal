using System.Text.Json.Serialization;

namespace DIBA_School_Portal.Server.Database_Entities
{
  
        public static class ApiExtensions
        {
            public static IServiceCollection AddApiLayer(this IServiceCollection services)
            {
                services
                    .AddControllers()
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(
                            new JsonStringEnumConverter());
                    });

                services.AddEndpointsApiExplorer();
                services.AddOpenApi();

                return services;
            }
        }
}
