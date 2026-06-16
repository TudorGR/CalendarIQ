using CalendarIQ.Api.Entities;

public class SmartSuggestionRequest
{
    public List<Event> PastEvents { get; set; } = new();
    public List<Event> FutureEvents { get; set; } = new();
    public DateTimeOffset CurrentDate { get; set; }
}