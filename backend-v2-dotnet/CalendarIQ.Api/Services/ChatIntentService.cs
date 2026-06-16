using System.Text.Json;
using CalendarIQ.Api.DTOs;
using System.Linq;

namespace CalendarIQ.Api.Services;

public class ChatIntentService : IChatIntentService
{
    private readonly IConversationMemoryService _conversationMemoryService;
    private readonly GroqClient _groqClient;

    public ChatIntentService(IConversationMemoryService conversationMemoryService, GroqClient groqClient)
    {
        _conversationMemoryService = conversationMemoryService;
        _groqClient = groqClient;
    }

    public async Task<GroqChatIntentResponse> DetermineIntentAsync(string text, int userId)
    {
        var now = DateTime.Now;
        var currentDate = now.ToString("yyyy-MM-dd");
        var currentTime = now.ToString("HH:mm");
        var currentDayName = now.DayOfWeek.ToString().ToLower();
        var dateMappingList = new List<string>();

        for (var i = 0; i < 7; i++)
        {
            var date = now.AddDays(i);
            dateMappingList.Add($"{date.DayOfWeek.ToString().ToLower()}: {date:yyyy-MM-dd}");
        }

        var systemPrompt = $$"""
You are a calendar assistant that outputs JSON only
Extract the function and its parameters from the user message, and provide a short but useful message for completion, missing parameters or just a friendly response
Consider the conversation history for context when processing the current user message

These are all your possible functions:
- create_event(title,startTime,endTime,date) (if the user asks to create or add or put a specific event)
- find_event(timeframe) (if the user asks about when an event will happen, or when was the last time an event happened, or when is the next time an event is gonna happen)
- suggest_time(title,date) (if the user asks to find the best time, for what time to schedule, or when to have an event)
- local_events(timeframe) (if the user asks about what events are happening, local events, or similar queries)
- delete_event(eventTime,eventTitle) (if the user asks to delete, remove, or cancel an event)
- unknown_function() (if the request is unclear)

FORMATS:
create_event format: {"function": "create_event", "parameters": ["title", "startTime", "endTime", "date"], "category": "category", "message": ""}
find_event format: {"function": "find_event", "timeframe": "timeframe", "category": "category", "message": ""}
suggest_time format: {"function": "suggest_time", "parameters": ["title", "date"], "category": "category", "message": ""}
local_events format: {"function": "local_events", "timeframe": "timeframe", "message": ""}
delete_event format: {"function": "delete_event", "parameters": ["eventTime", "eventTitle"], "message": ""}
unknown_function format: {"message": ""}

RULES:
create_event rules:
- the parameters should be in the order: title(string), startTime(24hr format), endTime(24hr format), date(YYYY-MM-DD)
- startTime and endTime must match regex ^([01]\d|2[0-3]):([0-5]\d)$
- some valid formats are these [add a (event) today] [put (event) from 10 to 12] etc. ex: "add meeting today"
- if you are not 100% sure about the start time or end time MAKE THEM EMPTY and ask for them
- you need all the parameters to create an event, ask for missing parameters

find_event rules:
- for timeframe choose either "future" or "past"
- category is gonna be the event category
- example formats: "when was my last meeting", "when is my next party"

suggest_time rules:
- the parameters should be in the order: title(string), date(YYYY-MM-DD)
- example formats: "when should I schedule a meeting?", "find a good time for my doctor appointment", "suggest time for studying"
- you don't need time for suggest_time, just title, date and category

local_events rules:
- for local_events, timeframe can be "today", "this week", "this month", day of week or a date

delete_event rules:
- parameters should be in the order: eventTime(string description of when), eventTitle(string description of what)
- example formats: "delete my meeting tomorrow", "remove doctor appointment next week", "cancel lunch on Friday"
- the eventTime can be a specific date, day of week, or relative time like "tomorrow", "next week"
- the eventTitle can be the exact title or a description like "meeting", "appointment"

General rules:
- startTime and endTime MUST be exactly HH:mm in 24-hour format (examples: "09:00", "13:30", "18:45")
- Empty parameters are gonna be "".
- Category must be one of these: Work, Education, Health & Wellness, Finance & Bills, Social & Family, Travel & Commute, Personal Tasks, Leisure & Hobbies, Other
- Date parameter is by default today: {{currentDate}} if it is not specified
- The current day of week is: {{currentDayName}}, and the time is: {{currentTime}}
- If a parameter is not specified or the request is unclear ask for clarification and make the parameter empty, DON'T ASSUME WHAT THE USER WANTS
- These are the next 7 days for context:
  {{string.Join("\n  ", dateMappingList)}}
- When a day of week is mentioned (like "monday", "tuesday", etc.) use the corresponding date from the mapping above
- Category parameter is mandatory, if you cannot deduce it use "Other"
- The time must be in 24 hour format
- Always provide a message
- Ensure the response is valid JSON: all keys and string values must use double quotes, and the structure must be parsable.
- Respect the formats and don't miss any fields
""";

        var conversationMemory = _conversationMemoryService.GetMessages(userId);

        var messages = new List<object>();
        messages.Add(new { role = "system", content = systemPrompt });
        foreach (var message in conversationMemory)
        {
            messages.Add(new { role = message.Role, content = message.Content });
        }

        messages.Add(new { role = "user", content = text });

        var response = await _groqClient.GetChatCompletionAsync(messages);

        var cleaned = response.Trim();

        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(7);
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Substring(3);
        }
        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3);
        }
        cleaned = cleaned.Trim();

        int startIndex = cleaned.IndexOfAny(new[] { '{', '[' });
        int endIndex = cleaned.LastIndexOfAny(new[] { '}', ']' });
        if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
        {
            cleaned = cleaned.Substring(startIndex, endIndex - startIndex + 1);
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<GroqChatIntentResponse>(cleaned, options);
            if (result == null) throw new Exception("Deserialization returned null.");

            if (result.Parameters == null || result.Parameters.Count == 0)
            {
                if (result.Function == "create_event")
                {
                    result.Parameters = new List<string> { result.Title ?? "", result.StartTime ?? "", result.EndTime ?? "", result.Date ?? "" };
                }
                else if (result.Function == "suggest_time")
                {
                    result.Parameters = new List<string> { result.Title ?? "", result.Date ?? "" };
                }
                else if (result.Function == "delete_event")
                {
                    result.Parameters = new List<string> { result.EventTime ?? "", result.EventTitle ?? "" };
                }
            }

            result.Parameters ??= new List<string>();
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing Groq intent JSON: {ex.Message}");
            Console.WriteLine($"Raw response: {response}");

            return new GroqChatIntentResponse
            {
                Function = "unknown_function",
                Message = "I couldn't understand that. Could you rephrase your request?"
            };
        }
    }
}