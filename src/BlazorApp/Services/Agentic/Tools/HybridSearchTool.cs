using System.Text;
using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using BlazorApp.Models;
using BlazorApp.Services.Embeddings;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace BlazorApp.Services.Agentic.Tools;

/// <summary>
/// A tool that performs Hybrid Search (Keyword + Vector) using Azure AI Search.
/// It automatically vectorizes the user's query to enable semantic understanding.
/// </summary>
public class HybridSearchTool : IAgenticTool
{
    private readonly string _searchEndpoint;
    private readonly TokenCredential _credential;
    private readonly IEmbeddingService _embeddingService;

    // We keep the same name "search_index" so the LLM knows how to use it 
    // just like the standard tool.
    public string Name => "search_index";

    public HybridSearchTool(IOptions<AzureSettings> settings, IEmbeddingService embeddingService)
    {
        _searchEndpoint = settings.Value.AzureSearch.Endpoint;
        _credential = new DefaultAzureCredential();
        _embeddingService = embeddingService;
    }

    public ChatTool GetDefinition()
    {
        // The definition remains similar to the standard search tool, 
        // as the complexity of vectorization is hidden from the LLM.
        return ChatTool.CreateFunctionTool(
            functionName: Name,
            functionDescription: "Searches the Azure AI Search index using Hybrid Search (Keyword + Vector). Use this to find assets based on descriptions, features, or concepts.",
            functionParameters: BinaryData.FromObjectAsJson(new
            {
                type = "object",
                properties = new
                {
                    query = new
                    {
                        type = "string",
                        description = "The search query string (e.g., 'compact separator for high pressure', 'pump vibration issues')."
                    }
                },
                required = new[] { "query" }
            })
        );
    }

    public async Task<ToolResult> ExecuteAsync(string arguments, string indexName)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(arguments);
        string queryText = args.GetProperty("query").GetString() ?? string.Empty;

        // 1. Generate Embedding for the query
        // The LLM gives us text, we convert it to a vector to find conceptually similar items.
        ReadOnlyMemory<float> queryVector = await _embeddingService.GenerateEmbeddingAsync(queryText);

        var searchClient = new SearchClient(new Uri(_searchEndpoint), indexName, _credential);

        // 2. Construct the Hybrid Query
        // We combine the text query (BM25) with the vector query (HNSW).
        var searchOptions = new SearchOptions
        {
            // Hybrid Search = Vector Query + Search Text
            VectorSearch = new VectorSearchOptions
            {
                Queries = { new VectorizedQuery(queryVector) { KNearestNeighborsCount = 50, Fields = { "descriptionVector" } } }
            },
            
            // Optional: Enable Semantic Reranking for even better results
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = "default"
            },
            
            Size = 5,
            Select = { "id", "name", "description", "assettypes", "region", "tags" } // Explicitly select fields to keep output clean
        };

        // Create a JSON representation of the query for the UI
        var queryJson = new
        {
            search = queryText,
            vectorQueries = new[]
            {
                new
                {
                    kind = "vector",
                    vector = "[...1536 dimensions...]",
                    fields = "descriptionVector",
                    k = 50
                }
            },
            queryType = "semantic",
            semanticConfiguration = "default",
            select = "id, name, description, assettypes, region, tags",
            top = 5
        };

        string queryDescription = JsonSerializer.Serialize(queryJson, new JsonSerializerOptions { WriteIndented = true });

        try 
        {
            // Execute the search
            SearchResults<SearchDocument> results = await searchClient.SearchAsync<SearchDocument>(queryText, searchOptions);
            
            var sb = new StringBuilder();
            
            await foreach (var result in results.GetResultsAsync())
            {
                sb.AppendLine("--- Document ---");
                sb.AppendLine($"Score: {result.Score}");
                // Reranker score is only available if semantic ranking is enabled and successful
                if (result.SemanticSearch.RerankerScore.HasValue)
                {
                    sb.AppendLine($"Reranker Score: {result.SemanticSearch.RerankerScore}");
                }

                foreach (var kvp in result.Document)
                {
                    // Skip the vector field in the output to the LLM to save tokens
                    if (kvp.Key == "descriptionVector") continue;
                    
                    sb.AppendLine($"{kvp.Key}: {kvp.Value}");
                }
                sb.AppendLine();
            }

            string output = sb.Length > 0 ? sb.ToString() : "No results found.";
            return new ToolResult { Output = output, QueryDescription = queryDescription };
        }
        catch (Exception ex)
        {
            return new ToolResult { Output = $"Error executing hybrid search: {ex.Message}", QueryDescription = queryDescription };
        }
    }
}
