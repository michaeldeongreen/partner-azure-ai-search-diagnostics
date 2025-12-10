using System.Text;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using BlazorApp.Models;
using Microsoft.Extensions.Options;

namespace BlazorApp.Services.Strategies;

public class SemanticSearchStrategy : ISearchStrategy
{
    private readonly IAiModelService _aiModelService;
    private readonly string _searchEndpoint;
    private readonly TokenCredential _credential;
    private readonly ILogger<SemanticSearchStrategy> _logger;

    public string StrategyType => "Semantic Search";

    public SemanticSearchStrategy(
        IAiModelService aiModelService,
        IOptions<AzureSettings> settings,
        ILogger<SemanticSearchStrategy> logger)
    {
        _aiModelService = aiModelService;
        _logger = logger;
        _searchEndpoint = settings.Value.AzureSearch.Endpoint;
        _credential = new DefaultAzureCredential();
    }

    public bool CanHandle(string indexName)
    {
        // Simple heuristic: if the name contains "semantic", use this strategy.
        // In a real app, we might check the index definition for semantic configurations.
        return indexName.Contains("semantic", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExecuteSearchAndChatAsync(string indexName, string userPrompt)
    {
        try
        {
            // 1. Search Azure AI Search
            var searchClient = new SearchClient(new Uri(_searchEndpoint), indexName, _credential);

            var searchOptions = new SearchOptions
            {
                QueryType = SearchQueryType.Semantic,
                SemanticSearch = new SemanticSearchOptions
                {
                    SemanticConfigurationName = "default",
                    // QueryCaption = new QueryCaption { CaptionType = QueryCaptionType.Extractive },
                    // QueryAnswer = new QueryAnswer { AnswerType = QueryAnswerType.Extractive }
                },
                Size = 5 // Top 5 results
            };

            // Note: We assume the semantic configuration is named 'default' or matches the index convention.
            // If the index was created with our tool, we might need to ensure the config name is known.
            // For now, let's try 'default'. If that fails, we might need to fetch the index definition to find the config name.
            
            SearchResults<SearchDocument> results = await searchClient.SearchAsync<SearchDocument>(userPrompt, searchOptions);

            var sb = new StringBuilder();
            await foreach (var result in results.GetResultsAsync())
            {
                // Extract relevant content. 
                // We'll dump the whole document for the LLM context, or specific fields if known.
                // Since schema is dynamic, we dump the dictionary.
                sb.AppendLine("--- Document ---");
                foreach (var kvp in result.Document)
                {
                    sb.AppendLine($"{kvp.Key}: {kvp.Value}");
                }
                
                // Add captions if available
                /*
                if (result.Captions != null)
                {
                    foreach (var caption in result.Captions)
                    {
                        sb.AppendLine($"Caption: {caption.Text}");
                    }
                }
                */
                sb.AppendLine();
            }

            string context = sb.ToString();

            if (string.IsNullOrWhiteSpace(context))
            {
                return "No relevant documents found in the search index to answer your question.";
            }

            // 2. Construct Prompt
            string systemPrompt = @"You are a helpful AI assistant. 
Use the provided context to answer the user's question. 
If the answer is not in the context, say you don't know.
Do not invent facts.";

            string fullUserPrompt = $@"Context:
{context}

Question:
{userPrompt}";

            // 3. Call LLM
            return await _aiModelService.GetChatCompletionAsync(systemPrompt, fullUserPrompt);
        }
        catch (RequestFailedException ex) when (ex.Message.Contains("Semantic configuration"))
        {
             _logger.LogError(ex, "Semantic configuration error");
             return "Error: Semantic search failed. Please ensure the index has a semantic configuration named 'default'.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SemanticSearchStrategy");
            throw;
        }
    }
}
