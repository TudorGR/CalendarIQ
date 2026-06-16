using CalendarIQ.Api.Data;

public interface ISchedulingService
{
    public Task<List<FreeSlot>> FindFreeSlotsForDay(long day, string workingHoursStart, string workingHoursEnd, int userId);
    public Task<CategoryPatterns?> GetCategoryPatternsAsync(string category, int userId);
    public List<RankedSlot> RankSlotsByPattern(List<FreeSlot> freeSlots, string category, int dayOfWeek, int duration, CategoryPatterns? patterns);
}