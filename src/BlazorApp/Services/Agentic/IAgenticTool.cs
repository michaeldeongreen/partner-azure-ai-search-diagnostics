using OpenAI.Chat;

namespace BlazorApp.Services.Agentic;

public interface IAgenticTool
{
    string Name { get; }
    ChatTool GetDefinition();
    Task<ToolResult> ExecuteAsync(string arguments, string indexName);
}
