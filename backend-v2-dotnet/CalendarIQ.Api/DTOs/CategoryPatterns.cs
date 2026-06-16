public class CategoryPatterns
{
    public int AverageDurationMinutes { get; set; }
    public int[] FrequencyByHour { get; set; } = new int[24];
    public Dictionary<int, int[]> FrequencyByDayOfWeek { get; set; } = new();

    public CategoryPatterns()
    {
        for (int i = 0; i < 7; i++)
        {
            FrequencyByDayOfWeek[i] = new int[24];
        }
    }
}