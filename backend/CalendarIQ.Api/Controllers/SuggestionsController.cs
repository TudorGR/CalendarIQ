using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using CalendarIQ.Api.Data;
using CalendarIQ.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalendarIQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuggestionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GroqClient _groqClient;
    private readonly ISuggestionsService _suggestionsService;

    public SuggestionsController(AppDbContext db, GroqClient groqClient, ISuggestionsService suggestionsService)
    {
        _db = db;
        _groqClient = groqClient;
        _suggestionsService = suggestionsService;
    }

    private int? GetCurrentUserId()
    {
        if (!Request.Headers.TryGetValue("x-auth-token", out var tokenValue))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenValue.FirstOrDefault());
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            return userIdClaim != null ? int.Parse(userIdClaim) : null;
        }
        catch
        {
            return null;
        }
    }

    [HttpPost("smart-suggestions")]
    public IActionResult GetSmartSuggestions([FromBody] SmartSuggestionRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { message = "Invalid or missing token" });

        try
        {
            var suggestions = _suggestionsService.GenerateSmartSuggestions(request);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error generating smart suggestions: " + ex.Message);
            return StatusCode(500, new { error = "Failed to generate smart suggestions" });
        }
    }

    [HttpPost("ai-suggestions")]
    public async Task<IActionResult> GetAiSuggestions([FromBody] AiSuggestionRequest request)
    {
        var selectedDateMs = ((DateTimeOffset)request.SelectedDate).ToUnixTimeMilliseconds();
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { message = "Invalid or missing token" });

        if (selectedDateMs == 0 || string.IsNullOrEmpty(request.TimeStart) || string.IsNullOrEmpty(request.TimeEnd))
        {
            return BadRequest(new { error = "Missing required parameters" });
        }

        long twoMonthsInMs = 2L * 30 * 24 * 60 * 60 * 1000;
        long twoMonthsAgo = selectedDateMs - twoMonthsInMs;

        int GetTimeInMinutes(string timeStr)
        {
            var parts = timeStr.Split(':');
            if (parts.Length != 2) return 0;
            return (int.Parse(parts[0]) * 60) + int.Parse(parts[1]);
        }

        int selectedStartMinutes = GetTimeInMinutes(request.TimeStart);
        int selectedEndMinutes = GetTimeInMinutes(request.TimeEnd);

        var pastEvents = await _db.Events
            .Where(a => a.UserId == userId && a.Day >= twoMonthsAgo && a.Day <= selectedDateMs)
            .OrderByDescending(e => e.Day)
            .ToListAsync();

        var relevantEvents = pastEvents.Where(e =>
        {
            int eventStart = GetTimeInMinutes(e.TimeStart);
            int eventEnd = GetTimeInMinutes(e.TimeEnd);

            return (eventStart < selectedEndMinutes && eventEnd > selectedStartMinutes) ||
                   (selectedStartMinutes < eventEnd && selectedEndMinutes > eventStart);
        }).ToList();

        if (!relevantEvents.Any())
        {
            return Ok(new { suggestions = new List<AiSuggestions>() });
        }

        var eventsForAI = relevantEvents.Take(50).Select(e => new
        {
            Title = e.Title,
            Category = e.Category,
            Location = e.Location,
            Day = DateTimeOffset.FromUnixTimeMilliseconds(e.Day).ToString("yyyy-MM-dd"),
            DayOfWeek = DateTimeOffset.FromUnixTimeMilliseconds(e.Day).ToString("dddd")
        }).ToList();

        var selectedDayFormatted = DateTimeOffset.FromUnixTimeMilliseconds(selectedDateMs).ToString("dddd, MMMM d");
        var eventsForAIJson = JsonSerializer.Serialize(eventsForAI, new JsonSerializerOptions { WriteIndented = true });

        var prompt = $@"Based on the user's past events, suggest 3-5 relevant event titles, categories, and locations for a new event.

Selected time slot: {request.TimeStart} - {request.TimeEnd} on {selectedDayFormatted}

Past events from the last 2 months that occurred during the time slot {request.TimeStart} - {request.TimeEnd}:
{eventsForAIJson}

Categories available: Work, Education, Health & Wellness, Finance & Bills, Social & Family, Travel & Commute, Personal Tasks, Leisure & Hobbies, Other

Please analyze patterns in the user's past events and suggest relevant events for this time slot. Consider:
1. Most frequent event types for this time slot
2. Recent events that might repeat
3. Common locations used
4. Typical categories for this time

Return ONLY a JSON array with this structure:
[
  {{
    ""suggestedTitle"": ""Event title"",
    ""category"": ""Category name"",
    ""suggestedLocation"": ""Location (can be empty string)"",
    ""confidence"": 0.8,
    ""reason"": ""Short explanation why this event is suggested (max 50 characters)""
  }}
]

Limit to 5 suggestions maximum, ordered by relevance/confidence. Keep reasons concise and specific.";

        var response = await _groqClient.GetChatCompletionAsync(new object[]
        {
            new { role = "system", content = "You are a helpful assistant that gives event suggestions based on user preferences and past behavior." },
            new { role = "user", content = prompt }
        });

        try
        {
            var cleaned = response
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var start = cleaned.IndexOfAny(new[] { '[', '{' });
            var end = cleaned.LastIndexOfAny(new[] { ']', '}' });
            if (start != -1 && end != -1)
                cleaned = cleaned.Substring(start, end - start + 1);

            using var doc = JsonDocument.Parse(cleaned);
            JsonElement root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("suggestions", out var listElement))
                root = listElement;

            var suggestions = JsonSerializer.Deserialize<List<AiSuggestions>>(root.GetRawText(),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return Ok(new { suggestions });
        }
        catch (Exception ex)
        {
            Console.WriteLine("AI PARSE ERROR: " + ex.Message);
            Console.WriteLine("RAW AI RESPONSE: " + response);

            return StatusCode(500, new
            {
                error = "Failed to parse AI response",
                raw = response
            });
        }
    }
}