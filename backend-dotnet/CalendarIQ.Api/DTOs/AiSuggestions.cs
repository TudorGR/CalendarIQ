public class AiSuggestions
{
    public string SuggestedTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SuggestedLocation { get; set; } = string.Empty;
    public double Confidence { get; set; } = 0.0;
    public string Reason { get; set; } = string.Empty;
}