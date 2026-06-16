namespace CalendarIQ.Api.DTOs;

public class CreateEventRequest
{
    public string? Title { get; set; }
    public long? Day { get; set; }
    public string? TimeStart { get; set; }
    public string? TimeEnd { get; set; }
    public string? Category { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public bool? Locked { get; set; }
    public bool? ReminderEnabled { get; set; }
    public int? ReminderTime { get; set; }
}