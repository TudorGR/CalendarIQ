
using CalendarIQ.Api.Data;
using CalendarIQ.Api.Entities;
using Microsoft.EntityFrameworkCore;

public class TimeService : ITimeService
{
    private readonly AppDbContext _context;
    public TimeService(AppDbContext context)
    {
        _context = context;
    }

    public string MinutesToTimeString(int minutes)
    {
        var hours = minutes / 60;
        var mins = minutes % 60;
        return $"{hours:D2}:{mins:D2}";
    }

    public int GetTimeInMinutes(string time)
    {
        var parts = time.Split(':');
        var hours = int.Parse(parts[0]);
        var minutes = int.Parse(parts[1]);
        return hours * 60 + minutes;
    }

    public async Task<List<Event>> CheckEventOverlaps(long day, string timeStart, string timeEnd, int userId)
    {
        var events = await _context.Events.Where(e => e.UserId == userId && e.Day == day).ToListAsync();

        var newStartMinutes = GetTimeInMinutes(timeStart);
        var newEndMinutes = GetTimeInMinutes(timeEnd);

        return events.Where(ev =>
        {
            var eventStartMinutes = GetTimeInMinutes(ev.TimeStart);
            var eventEndMinutes = GetTimeInMinutes(ev.TimeEnd);

            return newStartMinutes < eventEndMinutes &&
                eventStartMinutes < newEndMinutes;
        }).ToList();
    }
}