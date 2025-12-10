using BlazorApp.Services.Agentic.Prompts;
using BlazorApp.Services.Agentic.Tools;
using OpenAI.Chat;

namespace BlazorApp.Services.Agentic;

public class AgenticSearchService
{
    private readonly IAiModelService _aiModelService;
    private readonly IEnumerable<IAgenticTool> _tools;
    private readonly ILogger<AgenticSearchService> _logger;

    public AgenticSearchService(
        IAiModelService aiModelService,
        IEnumerable<IAgenticTool> tools,
        ILogger<AgenticSearchService> logger)
    {
        _aiModelService = aiModelService;
        _tools = tools;
        _logger = logger;
    }

    public async Task<AgenticChatResult> ChatAsync(string indexName, List<ChatMessage> history)
    {
        // Ensure system prompt is present
        if (!history.Any(m => m is SystemChatMessage))
        {
            history.Insert(0, new SystemChatMessage(AgentPrompts.SystemPrompt));
        }

        var options = new ChatCompletionOptions();
        foreach (var tool in _tools)
        {
            options.Tools.Add(tool.GetDefinition());
        }

        var toolsUsed = new List<string>();
        var toolQueries = new List<string>();

        try
        {
            bool requiresAction = true;
            while (requiresAction)
            {
                ChatCompletion completion = await _aiModelService.GetChatCompletionAsync(history, options);

                switch (completion.FinishReason)
                {
                    case ChatFinishReason.Stop:
                        // The model has finished generating the response
                        // Note: Content might be empty if it was just a tool call, but here it's Stop.
                        if (completion.Content != null && completion.Content.Count > 0)
                        {
                            history.Add(new AssistantChatMessage(completion.Content));
                            return new AgenticChatResult 
                            { 
                                Response = completion.Content[0].Text,
                                ToolsUsed = toolsUsed,
                                ToolQueries = toolQueries
                            };
                        }
                        return new AgenticChatResult { Response = "No response content.", ToolsUsed = toolsUsed, ToolQueries = toolQueries };

                    case ChatFinishReason.ToolCalls:
                        // The model wants to call a tool
                        history.Add(new AssistantChatMessage(completion.ToolCalls));

                        foreach (var toolCall in completion.ToolCalls)
                        {
                            toolsUsed.Add(toolCall.FunctionName);
                            var tool = _tools.FirstOrDefault(t => t.Name == toolCall.FunctionName);
                            if (tool != null)
                            {
                                _logger.LogInformation($"Executing tool: {tool.Name}");
                                var result = await tool.ExecuteAsync(toolCall.FunctionArguments.ToString(), indexName);
                                toolQueries.Add($"[{tool.Name}] {result.QueryDescription}");
                                history.Add(new ToolChatMessage(toolCall.Id, result.Output));
                            }
                            else
                            {
                                history.Add(new ToolChatMessage(toolCall.Id, "Tool not found."));
                            }
                        }
                        // Loop continues to send tool outputs back to the model
                        break;

                    default:
                        requiresAction = false;
                        return new AgenticChatResult 
                        { 
                            Response = $"Unexpected finish reason: {completion.FinishReason}",
                            ToolsUsed = toolsUsed,
                            ToolQueries = toolQueries
                        };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Agentic Chat Loop");
            return new AgenticChatResult { Response = "An error occurred while processing your request.", ToolsUsed = toolsUsed, ToolQueries = toolQueries };
        }

        return new AgenticChatResult { Response = "No response generated.", ToolsUsed = toolsUsed, ToolQueries = toolQueries };
    }
}
