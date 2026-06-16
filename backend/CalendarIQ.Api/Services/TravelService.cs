using System.Globalization;
using CalendarIQ.Api.DTOs;

namespace CalendarIQ.Api.Services;

public class TravelService
{
    private readonly HttpClient _httpClient;
    private readonly GroqClient _groqClient;

    public TravelService(HttpClient httpClient, GroqClient groqClient)
    {
        _httpClient = httpClient;
        _groqClient = groqClient;
    }

    public async Task<Coordinates> LocationToCoordinatesAsync(string location)
    {
        if (location == null)
            throw new ArgumentNullException(nameof(location));

        var prompt = $$"""
Convert this location text: "{{location}}" into precise latitude and longitude coordinates.

Respond only with a JSON object in this exact format:
{
    "latitude": [number],
    "longitude": [number]
}

If you can't determine the coordinates, respond with:
{
    "latitude": null,
    "longitude": null
}
""";

        var response = await _groqClient.GetChatCompletionAsync(new object[]
        {
        new { role = "system", content = "You are a helpful assistant that converts location names into geographic coordinates." },
        new { role = "user", content = prompt }
        });

        try
        {
            var coordinates = System.Text.Json.JsonSerializer.Deserialize<Coordinates>(response, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return coordinates ?? new Coordinates { Latitude = 0, Longitude = 0 };
        }
        catch (System.Text.Json.JsonException)
        {
            // If parsing fails, return null coordinates
            return new Coordinates { Latitude = 0, Longitude = 0 };
        }
    }

    public async Task<string> GetWeatherForecastAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");

        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);

        var url =
            $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&hourly=temperature_2m,precipitation,weathercode&forecast_days=1";

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return $"{{\"error\":\"Open-Meteo failed\",\"status\":{(int)response.StatusCode}}}";
        }

        return content;
    }
}