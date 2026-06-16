using CalendarIQ.Api.Entities;

public interface ITimeService
{
    public string MinutesToTimeString(int minutes);
    public int GetTimeInMinutes(string time);
    public Task<List<Event>> CheckEventOverlaps(long day, string timeStart, string timeEnd, int userId);
}