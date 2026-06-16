using System.Collections.Generic;
using CalendarIQ.Api.DTOs;

namespace CalendarIQ.Api.Services;

public interface IConversationMemoryService
{
    void AddMessage(int userId, string role, string content);
    List<ChatMessage> GetMessages(int userId);
    void ClearHistory(int userId);
}