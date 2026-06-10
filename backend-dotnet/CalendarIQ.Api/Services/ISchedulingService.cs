using CalendarIQ.Api.Data;

public interface ISchedulingService
{
    public Task<List<FreeSlot>> FindFreeSlotsForDay(long day, string workingHoursStart, string workingHoursEnd, int userId);
    public Task<CategoryPatterns?> GetCategoryPatternsAsync(string category, int userId, AppDbContext dbContext);
}