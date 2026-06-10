using System.Globalization;
using CalendarIQ.Api.DTOs;

public class EventValidator : IEventValidator
{
    private static readonly HashSet<string> ValidCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Other",
        "Work",
        "Education",
        "Health & Wellness",
        "Finance & Bills",
        "Social & Family",
        "Travel & Commute",
        "Personal Tasks",
        "Leisure & Hobbies",
    };

    public List<string> Validate(CreateEventRequest data)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(data.Title))
            errors.Add("title is required");

        if (data.Day == null)
            errors.Add("day is required");

        if (string.IsNullOrWhiteSpace(data.TimeStart))
            errors.Add("timeStart is required");

        if (string.IsNullOrWhiteSpace(data.TimeEnd))
            errors.Add("timeEnd is required");

        if (!string.IsNullOrWhiteSpace(data.TimeStart) && !TimeOnly.TryParseExact(data.TimeStart, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            errors.Add("timeStart must be HH:MM format");

        if (!string.IsNullOrWhiteSpace(data.TimeEnd) && !TimeOnly.TryParseExact(data.TimeEnd, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            errors.Add("timeEnd must be HH:MM format");

        if (TimeOnly.TryParseExact(data.TimeStart, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime) &&
            TimeOnly.TryParseExact(data.TimeEnd, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endTime))
        {
            if (endTime <= startTime)
                errors.Add("End time must be after start time");
        }

        if (data.Day != null)
        {
            try
            {
                var date = DateTimeOffset.FromUnixTimeMilliseconds(data.Day.Value).UtcDateTime.Date;
            }
            catch
            {
                errors.Add("day must be valid date");
            }
        }

        if (!string.IsNullOrWhiteSpace(data.Category) && !ValidCategories.Contains(data.Category))
        {
            errors.Add($"category must be one of: {string.Join(", ", ValidCategories)}");
        }

        return errors;
    }
}