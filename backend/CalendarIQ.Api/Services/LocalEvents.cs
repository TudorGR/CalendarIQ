using System.Text.Json;
using System.Text.RegularExpressions;
using CalendarIQ.Api.DTOs;

namespace CalendarIQ.Api.Services;

public class LocalEvents
{
    public async Task<List<LocalEvent>> GetLocalEvents(string city, string timeframe, GeminiClient geminiClient)
    {
        try
        {
            var prompt = $@"
What events are happening in {city} {timeframe}.
Today's date is {DateTime.UtcNow:yyyy-MM-dd}.

Return ONLY a JSON array with the following structure for each event:
[
  {{
    ""title"": ""Event Name"",
    ""description"": ""Short event description"",
    ""timeStart"": ""HH:MM"",
    ""timeEnd"": ""HH:MM"",
    ""day"": ""YYYY-MM-DD"",
    ""location"": ""Event venue/location"",
    ""category"": ""One of: Concert, Sports, Art, Food, Conference, Festival, Other""
  }}
]

Ensure all events have accurate dates in YYYY-MM-DD format.
Include only events that occur within the specified date range.
Do not include any explanatory text, only return valid JSON.
";

            var geminiResult = await geminiClient.GenerateContentAsync(prompt);

            var match = Regex.Match(geminiResult, @"\[\s*\{.*\}\s*\]", RegexOptions.Singleline);
            if (!match.Success)
            {
                throw new Exception("Failed to parse events data from Gemini response.");
            }

            var jsonString = match.Value;
            var events = JsonSerializer.Deserialize<List<LocalEvent>>(jsonString) ?? new List<LocalEvent>();

            var todayString = DateTime.UtcNow.ToString("yyyy-MM-dd");

            foreach (var evt in events)
            {
                if (string.IsNullOrWhiteSpace(evt.TimeStart)) evt.TimeStart = "00:00";
                if (string.IsNullOrWhiteSpace(evt.TimeEnd)) evt.TimeEnd = "23:59";
                if (string.IsNullOrWhiteSpace(evt.Day)) evt.Day = todayString;
            }

            return events;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching local events: {ex.Message}");
            return new List<LocalEvent>();
        }
    }
}