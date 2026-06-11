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

    public async Task<CategoryPatterns?> GetCategoryPatternsAsync(string category, int userId)
    {
        try
        {
            var today = DateTimeOffset.UtcNow;
            var threeMonthsAgoMs = today.AddMonths(-3).ToUnixTimeMilliseconds();
            var todayMs = today.ToUnixTimeMilliseconds();

            var pastEvents = await _context.Events
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

    public List<RankedSlot> RankSlotsByPattern(List<FreeSlot> freeSlots, string category, int dayOfWeek, int duration, CategoryPatterns? patterns)
    {
        if (patterns == null)
        {
            return freeSlots
                .Where(slot => slot.DurationMinutes >= duration)
                .Select(slot =>
                {
                    int startMins = _timeService.GetTimeInMinutes(slot.TimeStart);
                    return new RankedSlot
                    {
                        TimeStart = slot.TimeStart,
                        TimeEnd = _timeService.MinutesToTimeString(startMins + duration),
                        Score = 1,
                        DurationMinutes = duration
                    };
                })
                .OrderBy(slot => _timeService.GetTimeInMinutes(slot.TimeStart))
                .ToList();
        }

        var scoredSlots = new List<RankedSlot>();

        var filteredSlots = freeSlots.Where(slot => slot.DurationMinutes >= duration);

        foreach (var slot in filteredSlots)
        {
            int startMinutes = _timeService.GetTimeInMinutes(slot.TimeStart);
            int endMinutes = _timeService.GetTimeInMinutes(slot.TimeEnd);

            for (int min = startMinutes; min <= endMinutes - duration; min += 30)
            {
                string timeStart = _timeService.MinutesToTimeString(min);
                string timeEnd = _timeService.MinutesToTimeString(min + duration);

                int startHour = (int)Math.Floor((double)min / 60);
                int endHour = (int)Math.Ceiling((double)(min + duration) / 60);

                int daySpecificScore = 0;
                int generalScore = 0;

                for (int h = startHour; h < endHour; h++)
                {
                    if (h < 24)
                    {
                        int daySpecificValue = 0;
                        if (patterns.FrequencyByDayOfWeek.ContainsKey(dayOfWeek) &&
                            patterns.FrequencyByDayOfWeek[dayOfWeek].Length > h)
                        {
                            daySpecificValue = patterns.FrequencyByDayOfWeek[dayOfWeek][h];
                        }

                        int generalValue = patterns.FrequencyByHour.Length > h ? patterns.FrequencyByHour[h] : 0;

                        daySpecificScore += daySpecificValue * 3;
                        generalScore += generalValue;
                    }
                }
                int finalScore = daySpecificScore + generalScore;

                scoredSlots.Add(new RankedSlot
                {
                    TimeStart = timeStart,
                    TimeEnd = timeEnd,
                    Score = finalScore > 0 ? finalScore : 1,
                    DurationMinutes = duration
                });
            }
        }
        return scoredSlots.OrderByDescending(s => s.Score).ToList();
    }
}
