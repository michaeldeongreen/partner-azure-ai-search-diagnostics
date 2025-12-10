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

public class LookupTool : IAgenticTool
{
    private readonly string _searchEndpoint;
    private readonly TokenCredential _credential;

    public string Name => "get_document";

    public LookupTool(IOptions<AzureSettings> settings)
    {
        _searchEndpoint = settings.Value.AzureSearch.Endpoint;
        _credential = new DefaultAzureCredential();
    }

    public ChatTool GetDefinition()
    {
        // Sample Query: "Give me the full technical specs for asset-020."
        return ChatTool.CreateFunctionTool(
            functionName: Name,
            functionDescription: "Retrieves a specific document by its ID.",
            functionParameters: BinaryData.FromObjectAsJson(new
            {
                type = "object",
                properties = new
                {
                    id = new
                    {
                        type = "string",
                        description = "The unique identifier of the document (e.g., 'doc-001')."
                    }
                },
                required = new[] { "id" }
            })
        );
    }

    public async Task<ToolResult> ExecuteAsync(string arguments, string indexName)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(arguments);
        string id = args.GetProperty("id").GetString() ?? string.Empty;

        var searchClient = new SearchClient(new Uri(_searchEndpoint), indexName, _credential);
        
        // Create a JSON representation of the query for the UI
        // Note: GetDocumentAsync is a direct lookup, but we can represent it as a filter query or just a GET request
        var queryJson = new
        {
            operation = "GET",
            path = $"/indexes/{indexName}/docs/{id}",
            key = id
        };

        string queryDescription = JsonSerializer.Serialize(queryJson, new JsonSerializerOptions { WriteIndented = true });

        try
        {
            Response<SearchDocument> response = await searchClient.GetDocumentAsync<SearchDocument>(id);
            var doc = response.Value;
            return new ToolResult { Output = JsonSerializer.Serialize(doc), QueryDescription = queryDescription };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return new ToolResult { Output = "Document not found.", QueryDescription = queryDescription };
        }
        catch (Exception ex)
        {
            return new ToolResult { Output = $"Error retrieving document: {ex.Message}", QueryDescription = queryDescription };
        }
    }
}
