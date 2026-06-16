namespace CalendarIQ.Api.DTOs;

public class ChatRequest
{
    public string? Text { get; set; } 
    public string WorkingHoursStart{get;set;}="08:00";
    public string WorkingHoursEnd{get;set;}="20:00";
    
}