using CalendarIQ.Api.DTOs;

namespace CalendarIQ.Api.Services;

public interface IChatActionService
{
    Task<ChatResponse> ExecuteActionAsync(GroqChatIntentResponse intent, int userId, string workingHoursStart = "08:00", string workingHoursEnd = "20:00");
}
