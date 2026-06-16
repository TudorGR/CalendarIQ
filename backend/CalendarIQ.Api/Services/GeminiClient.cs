using System.Text;
using System.Text.Json;
using CalendarIQ.Api.Options;
using Microsoft.Extensions.Options;

namespace CalendarIQ.Api.Services;

public class GeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;

    public GeminiClient(HttpClient httpClient, IOptions<AiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> GenerateContentAsync(string prompt)
    {
        var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={_options.GeminiApiKey}";

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = "You are a helpful assistant that returns information about local events in structured JSON format." } }
            },
            tools = new[]
            {
                new { googleSearch = new { } }
            },
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var content = new StringContent(JsonSerializer.Serialize(payload, jsonOptions), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(requestUrl, content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);

        var textContent = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return textContent ?? string.Empty;
    }
}