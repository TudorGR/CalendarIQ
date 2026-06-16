using CalendarIQ.Api.Entities;

namespace CalendarIQ.Api.Services;

public interface ISuggestionsService
{
    List<SmartSuggestion> GenerateSmartSuggestions(SmartSuggestionRequest request);
}

public class SuggestionsService : ISuggestionsService
{
    public List<SmartSuggestion> GenerateSmartSuggestions(SmartSuggestionRequest request)
    {
        var today = request.CurrentDate.LocalDateTime;
        var suggestions = new List<SmartSuggestion>();

        // Get events from the past 2 months
        var twoMonthsAgo = today.AddMonths(-2);
        var pastMonthEvents = request.PastEvents
            .Where(e => DateTimeOffset.FromUnixTimeMilliseconds(e.Day).LocalDateTime >= twoMonthsAgo)
            .ToList();

        CreateEventSuggestions(suggestions, pastMonthEvents, today);
        FindEventSuggestions(suggestions, pastMonthEvents, request.FutureEvents, today);
        LocalEventSuggestions(suggestions, pastMonthEvents);
        TimeEventSuggestions(suggestions, pastMonthEvents, today);

        return DiversifyAndLimitSuggestions(suggestions);
    }

    private void CreateEventSuggestions(List<SmartSuggestion> suggestions, List<Event> pastMonthEvents, DateTime today)
    {
        var topCategories = pastMonthEvents
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? "Other" : e.Category)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .ToList();

        foreach (var group in topCategories)
        {
            if (group.Key == "Other") continue;

            var mostCommonTitle = group
                .GroupBy(e => e.Title)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? group.Key;

            var mostCommonDayStr = group
                .GroupBy(e => DateTimeOffset.FromUnixTimeMilliseconds(e.Day).ToString("dddd"))
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            if (Enum.TryParse<DayOfWeek>(mostCommonDayStr, out var mostCommonDay))
            {
                var nextDay = GetNextDayOfWeek(today, mostCommonDay);
                suggestions.Add(new SmartSuggestion
                {
                    Type = "CREATE_EVENT",
                    Suggestion = $"Schedule a {mostCommonTitle} on {nextDay:dddd} {nextDay:MMM d}"
                });
            }
        }
    }

    private void FindEventSuggestions(List<SmartSuggestion> suggestions, List<Event> pastMonthEvents, List<Event> futureEvents, DateTime today)
    {
        var oneMonthFromNow = today.AddMonths(1);
        var nextMonthEvents = futureEvents
            .Where(e => DateTimeOffset.FromUnixTimeMilliseconds(e.Day).LocalDateTime <= oneMonthFromNow)
            .ToList();

        var relevantEvents = pastMonthEvents.Concat(nextMonthEvents).ToList();
        var uniqueTitles = relevantEvents.Select(e => e.Title).Distinct().ToList();

        if (uniqueTitles.Any())
        {
            var selectedTitles = uniqueTitles.OrderBy(_ => Random.Shared.Next()).Take(2).ToList();

            foreach (var title in selectedTitles)
            {
                if (Random.Shared.NextDouble() < 0.5)
                {
                    suggestions.Add(new SmartSuggestion { Type = "FIND_EVENT", Suggestion = $"When is my next {title}?" });
                }
                else
                {
                    suggestions.Add(new SmartSuggestion { Type = "FIND_EVENT", Suggestion = $"When was my last {title}?" });
                }
            }
        }
    }

    private void LocalEventSuggestions(List<SmartSuggestion> suggestions, List<Event> pastMonthEvents)
    {
        var socialEvents = pastMonthEvents.Where(e => e.Category == "Social & Family").ToList();

        if (socialEvents.Any())
        {
            var commonDay = socialEvents
                .GroupBy(e => DateTimeOffset.FromUnixTimeMilliseconds(e.Day).ToString("dddd"))
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            if (!string.IsNullOrEmpty(commonDay))
            {
                suggestions.Add(new SmartSuggestion
                {
                    Type = "LOCAL_EVENTS",
                    Suggestion = $"What events are happening in the city this {commonDay.ToLower()}?"
                });
            }
        }

        var hardcodedOptions = new[]
        {
            "What events are happening in the city today?",
            "Show me local events for this week",
            "Any interesting events this weekend?"
        };

        suggestions.Add(new SmartSuggestion
        {
            Type = "LOCAL_EVENTS",
            Suggestion = hardcodedOptions[Random.Shared.Next(hardcodedOptions.Length)]
        });
    }

    private void TimeEventSuggestions(List<SmartSuggestion> suggestions, List<Event> pastMonthEvents, DateTime today)
    {
        var eventsByDay = pastMonthEvents
            .GroupBy(e => DateTimeOffset.FromUnixTimeMilliseconds(e.Day).ToString("dddd"))
            .ToList();

        var dayOptions = new List<(string Title, string DayOfWeek, string NextDate)>();

        foreach (var group in eventsByDay)
        {
            var topEvents = group
                .GroupBy(e => e.Title)
                .OrderByDescending(g => g.Count())
                .Take(2)
                .Select(g => g.Key);

            if (Enum.TryParse<DayOfWeek>(group.Key, out var dayOfWeek))
            {
                var nextDay = GetNextDayOfWeek(today, dayOfWeek);
                foreach (var title in topEvents)
                {
                    dayOptions.Add((title, group.Key, nextDay.ToString("MMM d")));
                }
            }
        }

        if (dayOptions.Any())
        {
            var selectedOptions = dayOptions.OrderBy(_ => Random.Shared.Next()).Take(2).ToList();
            foreach (var opt in selectedOptions)
            {
                suggestions.Add(new SmartSuggestion
                {
                    Type = "TIME_SUGGESTIONS",
                    Suggestion = $"When is the best time to schedule a {opt.Title} on {opt.DayOfWeek} {opt.NextDate}?"
                });
            }
        }
    }

    private List<SmartSuggestion> DiversifyAndLimitSuggestions(List<SmartSuggestion> suggestions)
    {
        var result = new List<SmartSuggestion>();
        var types = new[] { "CREATE_EVENT", "FIND_EVENT", "LOCAL_EVENTS", "TIME_SUGGESTIONS" };

        var shuffledTypes = types.OrderBy(_ => Random.Shared.Next()).ToList();
        var typesToInclude = Random.Shared.Next(2, 5); // Random number between 2-4
        var selectedTypes = shuffledTypes.Take(typesToInclude).ToList();

        foreach (var type in selectedTypes)
        {
            var typeOptions = suggestions.Where(s => s.Type == type).ToList();
            if (typeOptions.Any())
            {
                var numToAdd = Math.Min(Random.Shared.Next(1, 3), typeOptions.Count);
                result.AddRange(typeOptions.OrderBy(_ => Random.Shared.Next()).Take(numToAdd));
            }
        }

        // Ensure we have at least 3 suggestions
        if (result.Count < 3)
        {
            var remaining = suggestions
                .Where(s => !result.Any(r => r.Suggestion == s.Suggestion))
                .OrderBy(_ => Random.Shared.Next())
                .Take(3 - result.Count);

            result.AddRange(remaining);
        }

        // Final shuffle and cut to 5 max
        return result.OrderBy(_ => Random.Shared.Next()).Take(5).ToList();
    }

    private DateTime GetNextDayOfWeek(DateTime start, DayOfWeek target)
    {
        var next = start;
        for (int i = 0; i < 7; i++)
        {
            if (next.DayOfWeek == target) return next;
            next = next.AddDays(1);
        }
        return next;
    }
}