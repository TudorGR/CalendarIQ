using CalendarIQ.Api.DTOs;

namespace CalendarIQ.Api.Services;

public interface IChatIntentService
{
    Task<GroqChatIntentResponse> DetermineIntentAsync(string text, int userId);
}
