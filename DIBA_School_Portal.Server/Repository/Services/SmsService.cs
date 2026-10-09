using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace DIBA_School_Portal.Server.Services
{
    public class SmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;

        public SmsService(HttpClient httpClient, IConfiguration configuration, ILogger<SmsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var apiKey = _configuration["Termii:ApiKey"];
                var senderId = _configuration["Termii:SenderId"] ?? "N-Alert"; // Default Termii sender ID or your custom approved one

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("Termii API Key is missing in configuration.");
                    return false;
                }

                var payload = new
                {
                    api_key = apiKey,
                    to = phoneNumber,
                    from = senderId,
                    sms = message,
                    type = "plain",
                    channel = "dnd" // Uses DND route so messages deliver even if recipient has active telecom DND
                };

                // Termii SMS endpoint
                string url = "https://api.ng.termii.com/api/sms/send";

                var response = await _httpClient.PostAsJsonAsync(url, payload);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SMS successfully sent to {PhoneNumber} via Termii.", phoneNumber);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send SMS via Termii. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while sending SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }
        public async Task<bool> SendBulkSmsAsync(List<string> phoneNumbers, string message)
        {
            try
            {
                var apiKey = _configuration["Termii:ApiKey"];
                var senderId = _configuration["Termii:SenderId"] ?? "DIBA-Portal";

                if (string.IsNullOrEmpty(apiKey) || phoneNumbers == null || !phoneNumbers.Any())
                {
                    _logger.LogError("Termii API Key is missing or phone number list is empty.");
                    return false;
                }

                // Termii Bulk payload takes an array of strings in the 'to' field
                var payload = new
                {
                    api_key = apiKey,
                    to = phoneNumbers, // e.g. ["2348012345678", "2348098765432"]
                    from = senderId,
                    sms = message,
                    type = "plain",
                    channel = "dnd"
                };

                // Termii Bulk SMS endpoint
                string url = "https://api.ng.termii.com/api/sms/send/bulk";

                var response = await _httpClient.PostAsJsonAsync(url, payload);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Bulk SMS successfully dispatched to {Count} recipients via Termii.", phoneNumbers.Count);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send bulk SMS via Termii. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while sending bulk SMS.");
                return false;
            }
        }
    }
}