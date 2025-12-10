using System.Text;
using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using BlazorApp.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace BlazorApp.Services.Agentic.Tools;

public class SearchTool : IAgenticTool
{
    private readonly string _searchEndpoint;
    private readonly TokenCredential _credential;

    public string Name => "search_index";

    public SearchTool(IOptions<AzureSettings> settings)
    {
        _searchEndpoint = settings.Value.AzureSearch.Endpoint;
        _credential = new DefaultAzureCredential();
    }

    public ChatTool GetDefinition()
    {
        // Sample Query: "Find me high-efficiency compressors in Region 1."
        return ChatTool.CreateFunctionTool(
            functionName: Name,
            functionDescription: "Searches the Azure AI Search index for relevant documents using semantic search.",
            functionParameters: BinaryData.FromObjectAsJson(new
            {
                type = "object",
                properties = new
                {
                    query = new
                    {
                        type = "string",
                        description = "The search query string (e.g., 'battery maintenance', 'compressor failure')."
                    }
                },
                required = new[] { "query" }
            })
        );
    }

    public async Task<ToolResult> ExecuteAsync(string arguments, string indexName)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(arguments);
        string query = args.GetProperty("query").GetString() ?? string.Empty;

        var searchClient = new SearchClient(new Uri(_searchEndpoint), indexName, _credential);

        var searchOptions = new SearchOptions
        {
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = "default"
            },
            Size = 5
        };

        // Create a JSON representation of the query for the UI
        var queryJson = new
        {
            search = query,
            queryType = "semantic",
            semanticConfiguration = "default",
            top = 5
        };

        string queryDescription = JsonSerializer.Serialize(queryJson, new JsonSerializerOptions { WriteIndented = true });

        try 
        {
            SearchResults<SearchDocument> results = await searchClient.SearchAsync<SearchDocument>(query, searchOptions);
            var sb = new StringBuilder();
            
            await foreach (var result in results.GetResultsAsync())
            {
                sb.AppendLine("--- Document ---");
                foreach (var kvp in result.Document)
                {
                    sb.AppendLine($"{kvp.Key}: {kvp.Value}");
                }
                sb.AppendLine();
            }

            string output = sb.Length > 0 ? sb.ToString() : "No results found.";
            return new ToolResult { Output = output, QueryDescription = queryDescription };
        }
        catch (Exception ex)
        {
            return new ToolResult { Output = $"Error executing search: {ex.Message}", QueryDescription = queryDescription };
        }
    }
}
