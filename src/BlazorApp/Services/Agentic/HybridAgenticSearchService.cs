using BlazorApp.Services.Agentic.Tools;
using OpenAI.Chat;

namespace BlazorApp.Services.Agentic;

/// <summary>
/// A specialized version of the AgenticSearchService that uses the HybridSearchTool
/// instead of the standard SearchTool. This enables the agent to perform
/// vector-based semantic searches while retaining the ability to use other tools.
/// </summary>
public class HybridAgenticSearchService : AgenticSearchService
{
    // We inject the specific tools we want to use for this service.
    // This ensures isolation: this service ALWAYS uses HybridSearchTool,
    // regardless of what other tools are registered globally.
    public HybridAgenticSearchService(
        IAiModelService aiModelService,
        HybridSearchTool hybridTool,
        LookupTool lookupTool,
        StatsTool statsTool,
        ILogger<AgenticSearchService> logger)
        : base(aiModelService, new IAgenticTool[] { hybridTool, lookupTool, statsTool }, logger)
    {
    }
}
