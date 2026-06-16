using CalendarIQ.Api.Entities;

namespace CalendarIQ.Api.DTOs;

public class ChatResponse
{
    public string Intent { get; set; } = string.Empty;
    public Event? EventData { get; set; }
    public List<Event>? OverlappingEvents { get; set; }
    public List<RankedSlot>? SuggestedSlots { get; set; }
    public List<Event>? Events { get; set; }
    public Event? SelectedEvent { get; set; }
    public string? Category { get; set; }
    public string? Timeframe { get; set; }
    public string? City { get; set; }
    public List<LocalEvent>? LocalEvents { get; set; }
    public string Message { get; set; } = string.Empty;
}