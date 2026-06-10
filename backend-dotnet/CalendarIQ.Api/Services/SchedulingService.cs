using CalendarIQ.Api.Data;
using Microsoft.EntityFrameworkCore;

public class SchedulingService : ISchedulingService
{
    private readonly AppDbContext _context;
    private readonly ITimeService _timeService;

    public SchedulingService(AppDbContext context, ITimeService timeService)
    {
        _context = context;
        _timeService = timeService;
    }

    public async Task<List<FreeSlot>> FindFreeSlotsForDay(long day, string workingHoursStart, string workingHoursEnd, int userId)
    {
        var events = await _context.Events.Where(e => e.UserId == userId && e.Day == day).ToListAsync();

        var busyRanges = events.Select(e => new
        {
            TimeStart = _timeService.GetTimeInMinutes(e.TimeStart),
            TimeEnd = _timeService.GetTimeInMinutes(e.TimeEnd)
        }).OrderBy(e => e.TimeStart).ToList();

        var dayStart = _timeService.GetTimeInMinutes(workingHoursStart);
        var dayEnd = _timeService.GetTimeInMinutes(workingHoursEnd);

        var freeRanges = new List<FreeSlot>();
        var currentStart = dayStart;

        foreach (var range in busyRanges)
        {
            if (range.TimeStart > currentStart)
            {
                freeRanges.Add(new FreeSlot
                {
                    TimeStart = _timeService.MinutesToTimeString(currentStart),
                    TimeEnd = _timeService.MinutesToTimeString(range.TimeStart),
                    DurationMinutes = range.TimeStart - currentStart
                });
            }
            currentStart = Math.Max(currentStart, range.TimeEnd);
        }

        if (currentStart < dayEnd)
        {
            freeRanges.Add(new FreeSlot
            {
                TimeStart = _timeService.MinutesToTimeString(currentStart),
                TimeEnd = _timeService.MinutesToTimeString(dayEnd),
                DurationMinutes = dayEnd - currentStart
            });
        }

        return freeRanges.Where(r => r.DurationMinutes >= 30).ToList();
    }

    public async Task<CategoryPatterns?> GetCategoryPatternsAsync(string category, int userId, AppDbContext dbContext)
    {
        try
        {
            var today = DateTimeOffset.Now;
            var threeMonthsAgoMs = today.AddMonths(-3).ToUnixTimeMilliseconds();
            var todayMs = today.ToUnixTimeMilliseconds();

            var pastEvents = await dbContext.Events
            .Where(e => e.Category == category &&
                        e.UserId == userId &&
                        e.Day >= threeMonthsAgoMs &&
                        e.Day <= todayMs)
            .ToListAsync();

            if (!pastEvents.Any())
            {
                return null;
            }

            var patterns = new CategoryPatterns();
            int totalDuration = 0;
            int processedEvents = 0;

            foreach (var ev in pastEvents)
            {
                if (!TimeSpan.TryParse(ev.TimeStart, out var startTime) ||
                    !TimeSpan.TryParse(ev.TimeEnd, out var endTime))
                {
                    continue;
                }

                var duration = (endTime - startTime).TotalMinutes;
                totalDuration += (int)duration;
                processedEvents++;

                var eventDate = DateTimeOffset.FromUnixTimeMilliseconds(ev.Day).LocalDateTime;
                int dayOfWeek = (int)eventDate.DayOfWeek;

                int startHour = startTime.Hours;

                int endHour = endTime.Hours + (endTime.Minutes > 0 ? 1 : 0);

                for (int hour = startHour; hour < endHour; hour++)
                {
                    if (hour < 24)
                    {
                        patterns.FrequencyByHour[hour]++;
                        patterns.FrequencyByDayOfWeek[dayOfWeek][hour]++;
                    }
                }
            }

            if (processedEvents == 0)
            {
                return null;
            }

            patterns.AverageDurationMinutes = (int)Math.Round((double)totalDuration / processedEvents);

            return patterns;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting category patterns: {ex.Message}");
            return null;
        }
    }
}
