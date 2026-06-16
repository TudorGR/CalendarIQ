namespace CalendarIQ.Api.DTOs;

public class GroqChatIntentResponse
{
    public string Function { get; set; } = string.Empty;
    public List<string>? Parameters { get; set; }
    public string? Timeframe { get; set; }
    public string? Category { get; set; }
    public string Message { get; set; } = string.Empty;

    public string? Title { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Date { get; set; }
    public string? EventTime { get; set; }
    public string? EventTitle { get; set; }
}