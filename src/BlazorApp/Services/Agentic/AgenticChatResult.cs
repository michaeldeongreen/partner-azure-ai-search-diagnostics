namespace BlazorApp.Services.Agentic;

public class AgenticChatResult
{
    public string Response { get; set; } = string.Empty;
    public List<string> ToolsUsed { get; set; } = new();
    public List<string> ToolQueries { get; set; } = new();
}
