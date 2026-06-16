using CalendarIQ.Api.DTOs;

public interface IEventValidator
{
    List<string> Validate(CreateEventRequest data);
}