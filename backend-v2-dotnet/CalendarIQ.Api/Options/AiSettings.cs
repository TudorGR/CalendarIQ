namespace CalendarIQ.Api.Options;

public class AiOptions
{
    public const string SectionName = "AI";

    public string GroqApiKey { get; set; } = string.Empty;
    public string GroqModel { get; set; } = "llama-3.1-8b-instant";
    public string GeminiApiKey { get; set; } = string.Empty;
}