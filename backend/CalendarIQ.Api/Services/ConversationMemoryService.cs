using System.Collections.Concurrent;
using System.Collections.Generic;
using CalendarIQ.Api.DTOs;

namespace CalendarIQ.Api.Services;

public class ConversationMemoryService : IConversationMemoryService
{
    private readonly ConcurrentDictionary<int, List<ChatMessage>> _history = new();

    public void AddMessage(int userId, string role, string content)
    {
        var userHistory = _history.GetOrAdd(userId, _ => new List<ChatMessage>());

        lock (userHistory)
        {
            userHistory.Add(new ChatMessage { Role = role, Content = content });
            
            // Limit to last 10 messages (5 exchanges)
            if (userHistory.Count > 10)
            {
                userHistory.RemoveAt(0);
            }
        }
    }

    public List<ChatMessage> GetMessages(int userId)
    {
        var userHistory = _history.GetOrAdd(userId, _ => new List<ChatMessage>());

        lock (userHistory)
        {
            // Return a copy to avoid concurrent modification issues during enumeration
            return new List<ChatMessage>(userHistory);
        }
    }

    public void ClearHistory(int userId)
    {
        _history.TryRemove(userId, out _);
    }
}