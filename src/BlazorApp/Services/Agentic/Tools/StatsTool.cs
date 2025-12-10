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

public class StatsTool : IAgenticTool
{
    private readonly string _searchEndpoint;
    private readonly TokenCredential _credential;

    public string Name => "get_index_stats";

    public StatsTool(IOptions<AzureSettings> settings)
    {
        _searchEndpoint = settings.Value.AzureSearch.Endpoint;
        _credential = new DefaultAzureCredential();
    }

    public ChatTool GetDefinition()
    {
        // Sample Query: "How many regions are there?" or "What is the breakdown of asset types?"
        return ChatTool.CreateFunctionTool(
            functionName: Name,
            functionDescription: "Gets statistics about the index, including counts of documents by region or asset type.",
            functionParameters: BinaryData.FromObjectAsJson(new
            {
                type = "object",
                properties = new
                {
                    facet = new
                    {
                        type = "string",
                        description = "The field to facet by (e.g., 'region', 'assettypes'). Defaults to 'region'."
                    }
                }
            })
        );
    }

    public async Task<ToolResult> ExecuteAsync(string arguments, string indexName)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(arguments);
        string facet = "region";
        if (args.TryGetProperty("facet", out var facetProp))
        {
            facet = facetProp.GetString() ?? "region";
        }

        var searchClient = new SearchClient(new Uri(_searchEndpoint), indexName, _credential);

        var options = new SearchOptions
        {
            Size = 0 // We only want facets
        };
        
        // Request top 1000 to get all unique values
        options.Facets.Add($"{facet},count:1000");

        // Create a JSON representation of the query for the UI
        var queryJson = new
        {
            search = "*",
            facets = new[] { $"{facet},count:1000" },
            top = 0
        };

        string queryDescription = JsonSerializer.Serialize(queryJson, new JsonSerializerOptions { WriteIndented = true });

        try
        {
            SearchResults<SearchDocument> results = await searchClient.SearchAsync<SearchDocument>("*", options);
            
            if (results.Facets.ContainsKey(facet))
            {
                var facetResults = results.Facets[facet];
                var stats = facetResults.Select(f => new { Value = f.Value, Count = f.Count });
                return new ToolResult { Output = JsonSerializer.Serialize(stats), QueryDescription = queryDescription };
            }
            
            return new ToolResult { Output = $"No facets found for field '{facet}'.", QueryDescription = queryDescription };
        }
        catch (Exception ex)
        {
            return new ToolResult { Output = $"Error getting stats: {ex.Message}", QueryDescription = queryDescription };
        }
    }
}
