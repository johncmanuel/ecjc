using System.Text;
using System.Text.Json;

namespace server.Services;

// Lightweight wrapper around HttpClient to publish real-time events to Centrifugo's HTTP API.
public class CentrifugoService(IConfiguration configuration, ILogger<CentrifugoService> logger)
{
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly string _apiKey = configuration["Centrifugo:ApiKey"]
            ?? Environment.GetEnvironmentVariable("CENTRIFUGO_HTTP_API_KEY")
            ?? "centrifugo-dev-api-key";
    private readonly string _baseUrl = configuration["Centrifugo:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("CENTRIFUGO_BASE_URL")
            ?? "http://localhost:8000";
    private readonly ILogger<CentrifugoService> _logger = logger;

    public async Task PublishToUserAsync(string userId, object data)
    {
        // Channel format: "user:{userId}" (using the 'user' namespace configured in Centrifugo)
        var channel = $"user:{userId}";
        await PublishAsync(channel, data);
    }

    private async Task PublishAsync(string channel, object data)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                method = "publish",
                @params = new
                {
                    channel,
                    data
                }
            });

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-API-Key", _apiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Centrifugo publish failed ({StatusCode}): {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            // Centrifugo being down should never break the core app flow
            _logger.LogError(ex, "Failed to publish to Centrifugo channel {Channel}", channel);
        }
    }
}
