namespace CalendarIQ.Api.DTOs;

public class CreateEventRequest
{
    public string? Title { get; set; }
    public long? Day { get; set; }
    public string? TimeStart { get; set; }
    public string? TimeEnd { get; set; }
    public string? Category { get; set; }
}