using System.Text.Json;
using CalendarIQ.Api.Options;
using Microsoft.Extensions.Options;
using System.Text;

namespace CalendarIQ.Api.Services;

public class GroqClient
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public GroqClient(HttpClient httpClient, IOptions<AiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<string> GetChatCompletionAsync(IEnumerable<object> messages, double temperature = 0.1)
    {
        var requestUrl = "https://api.groq.com/openai/v1/chat/completions";
        var payload = new
        {
            model = _options.GroqModel,
            messages = messages,
            temperature = temperature,
            response_format = new { type = "json_object" }
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.GroqApiKey);

        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? "{}";
    }
}