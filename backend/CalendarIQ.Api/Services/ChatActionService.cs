using System.Globalization;
using System.Text.Json;
using CalendarIQ.Api.Data;
using CalendarIQ.Api.DTOs;
using CalendarIQ.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CalendarIQ.Api.Services;

public class ChatActionService : IChatActionService
{
    private readonly AppDbContext _context;
    private readonly ITimeService _timeService;
    private readonly ISchedulingService _schedulingService;
    private readonly LocalEvents _localEvents;
    private readonly GeminiClient _geminiClient;
    private readonly GroqClient _groqClient;
    private readonly IConversationMemoryService _conversationMemoryService;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public ChatActionService(
        AppDbContext context,
        ITimeService timeService,
        ISchedulingService schedulingService,
        LocalEvents localEvents,
        GeminiClient geminiClient,
        GroqClient groqClient,
        IConversationMemoryService conversationMemoryService
    )
    {
        _context = context;
        _timeService = timeService;
        _schedulingService = schedulingService;
        _localEvents = localEvents;
        _geminiClient = geminiClient;
        _groqClient = groqClient;
        _conversationMemoryService = conversationMemoryService;
    }

    public async Task<ChatResponse> ExecuteActionAsync(
        GroqChatIntentResponse intent,
        int userId,
        string workingHoursStart = "08:00",
        string workingHoursEnd = "20:00"
    )
    {
        switch (intent.Function)
        {
            case "create_event":
                return await HandleCreateEventAsync(
                    intent,
                    userId,
                    workingHoursStart,
                    workingHoursEnd
                );
            case "find_event":
                return await HandleFindEventAsync(intent, userId);
            case "suggest_time":
                return await HandleSuggestTimeAsync(
                    intent,
                    userId,
                    workingHoursStart,
                    workingHoursEnd
                );
            case "local_events":
                return await HandleLocalEventsAsync(intent, userId);
            case "delete_event":
                return await HandleDeleteEventAsync(intent, userId);
            case "unknown_function":
                return HandleUnknownFunction(intent, userId);
            default:
                return new ChatResponse { Message = intent.Message, Intent = "unknown" };
        }
    }

    private async Task<ChatResponse> HandleCreateEventAsync(
        GroqChatIntentResponse intent,
        int userId,
        string workingHoursStart,
        string workingHoursEnd
    )
    {
        var parameters = intent.Parameters;

        if (
            parameters != null
            && parameters.Count >= 4
            && parameters.All(p => !string.IsNullOrEmpty(p))
        )
        {
            var title = parameters[0];
            var timeStart = parameters[1];
            var timeEnd = parameters[2];
            var date = parameters[3];
            var category = intent.Category ?? "Other";

            var dayValue = DateTimeOffset.Parse(date).ToUnixTimeMilliseconds();

            var overlappingEvents = await _timeService.CheckEventOverlaps(
                dayValue,
                timeStart,
                timeEnd,
                userId
            );

            if (overlappingEvents.Count > 0)
            {
                var formattedDate = DateTimeOffset.Parse(date).ToString("dddd, MMMM d");
                var message =
                    $"I can't create \"{title}\" on {formattedDate} from {timeStart} to {timeEnd} because it would overlap with:";

                var eventData = new Event
                {
                    Title = title,
                    Description = "",
                    Day = dayValue,
                    TimeStart = timeStart,
                    TimeEnd = timeEnd,
                    Category = category,
                };

                _conversationMemoryService.AddMessage(
                    userId,
                    "assistant",
                    JsonSerializer.Serialize(
                        new { function = "create_event", message },
                        _jsonOptions
                    )
                );

                return new ChatResponse
                {
                    Intent = "event_overlap",
                    EventData = eventData,
                    OverlappingEvents = overlappingEvents
                        .Select(e => new Event
                        {
                            Id = e.Id,
                            Title = e.Title,
                            Day = e.Day,
                            TimeStart = e.TimeStart,
                            TimeEnd = e.TimeEnd,
                            Category = e.Category ?? "Other",
                            Location = e.Location ?? "",
                        })
                        .ToList(),
                    Message = message,
                };
            }
            else
            {
                var formattedDate = DateTimeOffset.Parse(date).ToString("dddd, MMMM d");
                var message = $"Event created: {title} on {formattedDate} at {timeStart}.";

                var eventData = new Event
                {
                    Title = title,
                    Description = "",
                    Day = dayValue,
                    TimeStart = timeStart,
                    TimeEnd = timeEnd,
                    Category = category,
                };

                _conversationMemoryService.AddMessage(
                    userId,
                    "assistant",
                    JsonSerializer.Serialize(
                        new { function = "create_event", message },
                        _jsonOptions
                    )
                );

                return new ChatResponse
                {
                    Intent = "create_event",
                    EventData = eventData,
                    Message = message,
                };
            }
        }

        var fallbackMessage = intent.Message ?? "I need more details to create that event.";
        _conversationMemoryService.AddMessage(
            userId,
            "assistant",
            JsonSerializer.Serialize(
                new { function = "create_event", message = fallbackMessage },
                _jsonOptions
            )
        );

        return new ChatResponse { Intent = "unknown", Message = fallbackMessage };
    }

    private async Task<ChatResponse> HandleFindEventAsync(GroqChatIntentResponse intent, int userId)
    {
        var timeframe = intent.Timeframe == "future" ? "future" : "past";
        var category = intent.Category ?? "Other";

        try
        {
            var matchingEvents = await FindEventsByCategoryAsync(category, timeframe, userId);

            if (matchingEvents.Count == 0)
            {
                var timeframeLabel = timeframe == "future" ? "coming up" : "in the past";
                var noEventsMessage = $"I couldn't find any {category} events {timeframeLabel}.";

                _conversationMemoryService.AddMessage(
                    userId,
                    "assistant",
                    JsonSerializer.Serialize(
                        new { function = "find_event", message = noEventsMessage },
                        _jsonOptions
                    )
                );

                return new ChatResponse
                {
                    Intent = "find_event_result",
                    Events = new List<Event>(),
                    Category = category,
                    Timeframe = timeframe,
                    Message = noEventsMessage,
                };
            }

            var formattedEvents = matchingEvents
                .Select(e => new
                {
                    id = e.Id,
                    title = e.Title,
                    day = DateTimeOffset.FromUnixTimeMilliseconds(e.Day).ToString("yyyy-MM-dd"),
                    timeStart = e.TimeStart,
                    timeEnd = e.TimeEnd,
                    category = e.Category ?? "Other",
                    location = e.Location ?? "",
                })
                .ToList();

            var sortedEvents =
                timeframe == "future"
                    ? formattedEvents.OrderBy(e => e.day).ThenBy(e => e.timeStart).ToList()
                    : formattedEvents
                        .OrderByDescending(e => e.day)
                        .ThenByDescending(e => e.timeStart)
                        .ToList();

            var currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            var matchPrompt = $$"""
                You are an AI assistant that helps find the most relevant event from a list based on a user's query.

                Category: {{category}}
                Timeframe: {{timeframe}}
                Current date and time: {{currentDateTime}}

                Available events:
                {{JsonSerializer.Serialize(sortedEvents, _jsonOptions)}}

                Please analyze these events and determine which ONE is most relevant to the user's query.
                Consider factors like event title, timing, and any specific details.

                Your response must be a JSON that follows this exact format:
                {
                  "eventId": "id of the selected event",
                  "message": "A short message of when you found the event."
                }
                """;

            var groqResponseStr = await _groqClient.GetChatCompletionAsync(
                new[] { new { role = "user", content = matchPrompt } },
                temperature: 0
            );

            var matchResult = JsonSerializer.Deserialize<GroqMatchResponse>(
                groqResponseStr,
                _jsonOptions
            );

            if (matchResult?.EventId == null)
            {
                return new ChatResponse
                {
                    Intent = "find_event_result",
                    Events = matchingEvents,
                    SelectedEvent = null,
                    Category = category,
                    Timeframe = timeframe,
                    Message = matchResult?.Message ?? "No matching event found.",
                };
            }

            var selectedEvent = matchingEvents.FirstOrDefault(e => e.Id == matchResult.EventId);

            var responseMessage = matchResult.Message;
            _conversationMemoryService.AddMessage(
                userId,
                "assistant",
                JsonSerializer.Serialize(
                    new { function = "find_event", message = responseMessage },
                    _jsonOptions
                )
            );

            return new ChatResponse
            {
                Intent = "find_event_result",
                Events = matchingEvents,
                SelectedEvent = selectedEvent,
                Category = category,
                Timeframe = timeframe,
                Message = responseMessage,
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding events: {ex.Message}");
            return new ChatResponse
            {
                Intent = "error",
                Message = "Sorry, I encountered an error while searching for events.",
            };
        }
    }

    private async Task<ChatResponse> HandleSuggestTimeAsync(
        GroqChatIntentResponse intent,
        int userId,
        string workingHoursStart,
        string workingHoursEnd
    )
    {
        var parameters = intent.Parameters;

        if (parameters != null && parameters.Count >= 2)
        {
            var title = parameters[0];
            var date = parameters[1];

            if (string.IsNullOrEmpty(title))
            {
                var responseMessage =
                    intent.Message
                    ?? "Please provide a title for the event I should find time for.";
                _conversationMemoryService.AddMessage(
                    userId,
                    "assistant",
                    JsonSerializer.Serialize(
                        new { function = "suggest_time", message = responseMessage },
                        _jsonOptions
                    )
                );
                return new ChatResponse { Message = responseMessage, Intent = "unknown" };
            }

            if (string.IsNullOrEmpty(date) || !DateOnly.TryParse(date, out _))
            {
                var responseMessage =
                    intent.Message ?? "I need a valid date to suggest optimal times.";
                _conversationMemoryService.AddMessage(
                    userId,
                    "assistant",
                    JsonSerializer.Serialize(
                        new { function = "suggest_time", message = responseMessage },
                        _jsonOptions
                    )
                );
                return new ChatResponse { Message = responseMessage, Intent = "unknown" };
            }

            var dayValue = DateTimeOffset.Parse(date).ToUnixTimeMilliseconds();
            var eventCategory = intent.Category ?? "Other";
            var dayOfWeek = (int)DateOnly.Parse(date).DayOfWeek; // 0 = Sunday, 6 = Saturday

            var freeSlots = await _schedulingService.FindFreeSlotsForDay(
                dayValue,
                workingHoursStart,
                workingHoursEnd,
                userId
            );

            var categoryPatterns = await _schedulingService.GetCategoryPatternsAsync(
                eventCategory,
                userId
            );

            var avgDuration =
                categoryPatterns != null
                    ? Math.Max(
                        (int)Math.Ceiling(categoryPatterns.AverageDurationMinutes / 30.0) * 30,
                        30
                    )
                    : 60;

            var rankedSlots = _schedulingService.RankSlotsByPattern(
                freeSlots,
                eventCategory,
                dayOfWeek,
                avgDuration,
                categoryPatterns
            );

            var formattedDate = DateTimeOffset.Parse(date).ToString("yyyy-MM-dd");
            var message =
                $"Here are the best times to schedule \"{title}\" on {formattedDate} based on your patterns.";

            _conversationMemoryService.AddMessage(
                userId,
                "assistant",
                JsonSerializer.Serialize(new { function = "suggest_time", message }, _jsonOptions)
            );

            return new ChatResponse
            {
                Intent = "time_suggestions",
                EventData = new Event
                {
                    Title = title,
                    Description = "",
                    Day = dayValue,
                    Category = eventCategory,
                },
                SuggestedSlots = rankedSlots
                    .Select(
                        (slot, index) =>
                            new RankedSlot
                            {
                                TimeStart = slot.TimeStart,
                                TimeEnd = slot.TimeEnd,
                                Score = slot.Score,
                                DurationMinutes = slot.DurationMinutes,
                            }
                    )
                    .ToList(),
                Message = message,
            };
        }
        else
        {
            var message = "I need both an event title and a date to suggest optimal times.";
            _conversationMemoryService.AddMessage(
                userId,
                "assistant",
                JsonSerializer.Serialize(new { function = "suggest_time", message }, _jsonOptions)
            );
            return new ChatResponse { Message = message, Intent = "unknown" };
        }
    }

    private async Task<ChatResponse> HandleLocalEventsAsync(
        GroqChatIntentResponse intent,
        int userId
    )
    {
        var timeframe = intent.Timeframe ?? "this week";
        var cityName = "Iasi Romania"; // City is hardcoded as specified in requirements

        try
        {
            var events = await _localEvents.GetLocalEvents(cityName, timeframe, _geminiClient);

            var message = $"Here are the events in {cityName} for {timeframe}";
            _conversationMemoryService.AddMessage(
                userId,
                "assistant",
                JsonSerializer.Serialize(new { function = "local_events", message }, _jsonOptions)
            );

            return new ChatResponse
            {
                Intent = "local_events",
                LocalEvents = events,
                City = cityName,
                Timeframe = timeframe,
                Message = message,
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching local events: {ex.Message}");
            return new ChatResponse
            {
                Intent = "error",
                Message = "Sorry, I couldn't fetch the local events at this time.",
            };
        }
    }

    private async Task<ChatResponse> HandleDeleteEventAsync(
        GroqChatIntentResponse intent,
        int userId
    )
    {
        var parameters = intent.Parameters;

        if (parameters != null && parameters.Count >= 2)
        {
            var eventTime = parameters[0];
            var eventTitle = parameters[1];

            if (string.IsNullOrEmpty(eventTime) || string.IsNullOrEmpty(eventTitle))
            {
                var fallbackMessage =
                    intent.Message ?? "I need more details about which event to delete.";
                _conversationMemoryService.AddMessage(
                    userId,
                    "assistant",
                    JsonSerializer.Serialize(
                        new { function = "delete_event", message = fallbackMessage },
                        _jsonOptions
                    )
                );
                return new ChatResponse { Message = fallbackMessage, Intent = "unknown" };
            }

            try
            {
                var today = DateTimeOffset.UtcNow.Date;
                var thirtyDaysLater = today.AddDays(30);
                var todayMs = new DateTimeOffset(today).ToUnixTimeMilliseconds();
                var thirtyDaysMs = new DateTimeOffset(thirtyDaysLater).ToUnixTimeMilliseconds();

                var upcomingEvents = await _context
                    .Events.Where(e =>
                        e.UserId == userId && e.Day >= todayMs && e.Day <= thirtyDaysMs
                    )
                    .ToListAsync();

                if (upcomingEvents.Count == 0)
                {
                    var noEventsMessage = "I couldn't find any events in the next 30 days.";
                    _conversationMemoryService.AddMessage(
                        userId,
                        "assistant",
                        JsonSerializer.Serialize(
                            new { function = "delete_event", message = noEventsMessage },
                            _jsonOptions
                        )
                    );
                    return new ChatResponse
                    {
                        Intent = "delete_event_result",
                        Events = new List<Event>(),
                        Message = noEventsMessage,
                    };
                }

                var formattedEvents = upcomingEvents
                    .Select(e => new
                    {
                        id = e.Id,
                        title = e.Title,
                        day = DateTimeOffset.FromUnixTimeMilliseconds(e.Day).ToString("yyyy-MM-dd"),
                        dayOfWeek = DateTimeOffset.FromUnixTimeMilliseconds(e.Day).ToString("dddd"),
                        timeStart = e.TimeStart,
                        timeEnd = e.TimeEnd,
                        category = e.Category ?? "Other",
                        location = e.Location ?? "",
                    })
                    .ToList();

                var deletePrompt = $$"""
                    You are an AI assistant that helps identify which event to delete based on a user's description.

                    User wants to delete an event described as: "{{eventTitle}}" on/at "{{eventTime}}"

                    Current date: {{DateTime.Today.ToString("yyyy-MM-dd")}}
                    Current day of week: {{DateTime.Today.ToString("dddd")}}

                    Available events in the next 30 days:
                    {{JsonSerializer.Serialize(formattedEvents, _jsonOptions)}}

                    Find the SINGLE most likely event that matches the user's description.

                    Consider the event title, date, and any other relevant details from the user's request.

                    Your response must be a JSON with this format:
                    {
                      "eventId": "id of the event to delete",
                      "message": "A short and natural confirmation message about which event will be deleted."
                    }

                    If you cannot find a matching event, use null for eventId and explain why in the message.
                    """;

                var groqResponseStr = await _groqClient.GetChatCompletionAsync(
                    new[] { new { role = "user", content = deletePrompt } },
                    temperature: 0
                );

                if (string.IsNullOrEmpty(groqResponseStr))
                {
                    var errorMessage = "I couldn't find that specific event.";
                    _conversationMemoryService.AddMessage(
                        userId,
                        "assistant",
                        JsonSerializer.Serialize(
                            new { function = "delete_event", message = errorMessage },
                            _jsonOptions
                        )
                    );
                    return new ChatResponse { Message = errorMessage, Intent = "delete_event" };
                }

                var matchResult = JsonSerializer.Deserialize<GroqMatchResponse>(
                    groqResponseStr,
                    _jsonOptions
                );

                if (matchResult?.EventId == null)
                {
                    return new ChatResponse
                    {
                        Intent = "delete_event_result",
                        Events = upcomingEvents,
                        SelectedEvent = null,
                        Message = matchResult?.Message ?? "I couldn't find that specific event.",
                    };
                }

                var selectedEvent = upcomingEvents.FirstOrDefault(e => e.Id == matchResult.EventId);

                if (selectedEvent == null)
                {
                    return new ChatResponse
                    {
                        Intent = "delete_event_result",
                        Events = upcomingEvents,
                        SelectedEvent = null,
                        Message = "I found an event ID but couldn't retrieve the event details.",
                    };
                }

                var responseMessage = matchResult.Message;
                _conversationMemoryService.AddMessage(
                    userId,
                    "assistant",
                    JsonSerializer.Serialize(
                        new { function = "delete_event", message = responseMessage },
                        _jsonOptions
                    )
                );

                return new ChatResponse
                {
                    Intent = "delete_event_result",
                    SelectedEvent = selectedEvent,
                    Message = responseMessage,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding event to delete: {ex.Message}");
                return new ChatResponse
                {
                    Intent = "error",
                    Message =
                        "Sorry, I encountered an error while trying to find the event to delete.",
                };
            }
        }

        var fallback = intent.Message ?? "I need more details about which event to delete.";
        _conversationMemoryService.AddMessage(
            userId,
            "assistant",
            JsonSerializer.Serialize(
                new { function = "delete_event", message = fallback },
                _jsonOptions
            )
        );
        return new ChatResponse { Message = fallback, Intent = "unknown" };
    }

    private ChatResponse HandleUnknownFunction(GroqChatIntentResponse intent, int userId)
    {
        var message = intent.Message ?? "I'm not sure what you're asking. Could you rephrase?";
        _conversationMemoryService.AddMessage(
            userId,
            "assistant",
            JsonSerializer.Serialize(new { function = "unknown", message }, _jsonOptions)
        );

        return new ChatResponse { Intent = "unknown", Message = message };
    }

    private async Task<List<Event>> FindEventsByCategoryAsync(
        string category,
        string timeframe,
        int userId
    )
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var today = now.Date;
            var currentTime = now.ToString("HH:mm");

            long startDateMs,
                endDateMs;

            if (timeframe == "future")
            {
                startDateMs = new DateTimeOffset(today.AddDays(-1)).ToUnixTimeMilliseconds();
                endDateMs = new DateTimeOffset(today.AddMonths(3)).ToUnixTimeMilliseconds();
            }
            else
            {
                startDateMs = new DateTimeOffset(today.AddMonths(-3)).ToUnixTimeMilliseconds();
                endDateMs = new DateTimeOffset(today.AddDays(1)).ToUnixTimeMilliseconds();
            }

            var events = await _context
                .Events.Where(e =>
                    e.UserId == userId
                    && e.Category == category
                    && e.Day >= startDateMs
                    && e.Day <= endDateMs
                )
                .OrderBy(e => e.Day)
                .ThenBy(e => e.TimeStart)
                .ToListAsync();

            var todayString = today.ToString("yyyy-MM-dd");
            return events
                .Where(e =>
                {
                    var eventDate = DateTimeOffset
                        .FromUnixTimeMilliseconds(e.Day)
                        .ToString("yyyy-MM-dd");
                    var isToday = eventDate == todayString;

                    if (!isToday)
                    {
                        return timeframe == "future"
                            ? e.Day > new DateTimeOffset(today).ToUnixTimeMilliseconds()
                            : e.Day < new DateTimeOffset(today).ToUnixTimeMilliseconds();
                    }

                    return timeframe == "future"
                        ? string.Compare(e.TimeStart, currentTime, StringComparison.Ordinal) >= 0
                        : string.Compare(e.TimeStart, currentTime, StringComparison.Ordinal) < 0;
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding events by category: {ex.Message}");
            return new List<Event>();
        }
    }
}
