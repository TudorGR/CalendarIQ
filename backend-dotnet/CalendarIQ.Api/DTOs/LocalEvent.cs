using System.Text.Json.Serialization;

namespace CalendarIQ.Api.DTOs;

public class LocalEvent
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("timeStart")]
    public string? TimeStart { get; set; }

    [JsonPropertyName("timeEnd")]
    public string? TimeEnd { get; set; }

    [JsonPropertyName("day")]
    public string? Day { get; set; }

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}