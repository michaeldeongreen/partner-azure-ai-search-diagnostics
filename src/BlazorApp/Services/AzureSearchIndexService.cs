using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Identity;
using BlazorApp.Models;
using Microsoft.Extensions.Options;

namespace BlazorApp.Services;

/// <summary>
/// Service for managing Azure AI Search indexes.
/// Follows Single Responsibility Principle - handles only index operations.
/// Follows Dependency Inversion Principle - depends on abstractions (IOptions, configuration).
/// </summary>
public class AzureSearchIndexService : IAzureSearchIndexService
{
    private readonly string _searchEndpoint;
    private readonly TokenCredential _credential;
    private readonly ILogger<AzureSearchIndexService> _logger;
    private readonly HttpClient _httpClient;

    public AzureSearchIndexService(
        IOptions<AzureSettings> settings,
        ILogger<AzureSearchIndexService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var azureSettings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        
        if (string.IsNullOrWhiteSpace(azureSettings.AzureSearch?.Endpoint))
        {
            throw new InvalidOperationException("Azure Search endpoint is not configured.");
        }

        _searchEndpoint = azureSettings.AzureSearch.Endpoint.TrimEnd('/');
        _credential = new DefaultAzureCredential();
        _httpClient = httpClientFactory.CreateClient();
    }

    public IndexValidationResult ValidateIndexJson(string indexJson)
    {
        var result = new IndexValidationResult();

        if (string.IsNullOrWhiteSpace(indexJson))
        {
            result.IsValid = false;
            result.Message = "Index JSON cannot be empty.";
            result.Errors.Add("Empty JSON provided.");
            return result;
        }

        try
        {
            using var document = JsonDocument.Parse(indexJson);
            var root = document.RootElement;

            // Validate required properties
            if (!root.TryGetProperty("name", out var nameElement))
            {
                result.Errors.Add("Missing required property: 'name'");
            }
            else if (string.IsNullOrWhiteSpace(nameElement.GetString()))
            {
                result.Errors.Add("Index 'name' cannot be empty.");
            }

            if (!root.TryGetProperty("fields", out var fieldsElement))
            {
                result.Errors.Add("Missing required property: 'fields'");
            }
            else if (fieldsElement.ValueKind != JsonValueKind.Array || fieldsElement.GetArrayLength() == 0)
            {
                result.Errors.Add("'fields' must be a non-empty array.");
            }

            result.IsValid = result.Errors.Count == 0;
            result.Message = result.IsValid 
                ? "Index JSON is valid." 
                : $"Validation failed with {result.Errors.Count} error(s).";
        }
        catch (JsonException ex)
        {
            result.IsValid = false;
            result.Message = "Invalid JSON format.";
            result.Errors.Add($"JSON parsing error: {ex.Message}");
            _logger.LogWarning(ex, "Failed to parse index JSON during validation");
        }

        return result;
    }

    public async Task<IndexOperationResult> CreateIndexAsync(string indexJson)
    {
        var result = new IndexOperationResult();

        try
        {
            // First validate the JSON
            var validationResult = ValidateIndexJson(indexJson);
            if (!validationResult.IsValid)
            {
                result.Success = false;
                result.Message = $"Validation failed: {string.Join(", ", validationResult.Errors)}";
                _logger.LogWarning("Index creation failed due to validation errors: {Errors}", 
                    string.Join(", ", validationResult.Errors));
                return result;
            }

            // Parse the JSON to extract the index name
            using var document = JsonDocument.Parse(indexJson);
            var indexName = document.RootElement.GetProperty("name").GetString();

            if (string.IsNullOrWhiteSpace(indexName))
            {
                result.Success = false;
                result.Message = "Index name is required.";
                return result;
            }

            _logger.LogInformation("Creating search index: {IndexName}", indexName);

            // Get access token for Azure Search
            var tokenRequestContext = new TokenRequestContext(new[] { "https://search.azure.com/.default" });
            var token = await _credential.GetTokenAsync(tokenRequestContext, default);

            // Create HTTP request to Azure Search REST API
            var apiVersion = "2024-07-01";
            var url = $"{_searchEndpoint}/indexes?api-version={apiVersion}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            request.Content = new StringContent(indexJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                result.Success = true;
                result.IndexName = indexName;
                result.Message = $"Index '{indexName}' created successfully.";
                _logger.LogInformation("Successfully created index: {IndexName}", indexName);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                result.Success = false;
                result.Message = "An index with this name already exists. Please use a different name or delete the existing index first.";
                _logger.LogWarning("Index creation failed - index already exists");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                result.Success = false;
                result.Message = $"Azure Search error (HTTP {(int)response.StatusCode}): {errorContent}";
                _logger.LogError("Azure Search request failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
            }
        }
        catch (JsonException ex)
        {
            result.Success = false;
            result.Message = $"JSON error: {ex.Message}";
            _logger.LogError(ex, "JSON parsing error while creating index");
        }
        catch (HttpRequestException ex)
        {
            result.Success = false;
            result.Message = $"Network error: {ex.Message}";
            _logger.LogError(ex, "HTTP request error while creating index");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Unexpected error: {ex.Message}";
            _logger.LogError(ex, "Unexpected error while creating index");
        }

        return result;
    }

    public async Task<List<string>> ListIndexesAsync()
    {
        var indexes = new List<string>();
        try
        {
            var tokenRequestContext = new TokenRequestContext(new[] { "https://search.azure.com/.default" });
            var token = await _credential.GetTokenAsync(tokenRequestContext, default);

            var apiVersion = "2024-07-01";
            var url = $"{_searchEndpoint}/indexes?api-version={apiVersion}&$select=name";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            
            if (document.RootElement.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in valueElement.EnumerateArray())
                {
                    if (element.TryGetProperty("name", out var nameElement))
                    {
                        indexes.Add(nameElement.GetString() ?? string.Empty);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing indexes");
            throw;
        }
        return indexes;
    }

    public async Task UploadDocumentsAsync(string indexName, List<string> jsonDocuments)
    {
        if (string.IsNullOrWhiteSpace(indexName)) throw new ArgumentNullException(nameof(indexName));
        if (jsonDocuments == null || !jsonDocuments.Any()) return;

        try
        {
            var tokenRequestContext = new TokenRequestContext(new[] { "https://search.azure.com/.default" });
            var token = await _credential.GetTokenAsync(tokenRequestContext, default);

            var apiVersion = "2024-07-01";
            var url = $"{_searchEndpoint}/indexes/{indexName}/docs/index?api-version={apiVersion}";

            var batchItems = new List<System.Text.Json.Nodes.JsonNode>();

            foreach (var json in jsonDocuments)
            {
                var node = System.Text.Json.Nodes.JsonNode.Parse(json);
                if (node is System.Text.Json.Nodes.JsonObject obj)
                {
                    obj["@search.action"] = "upload";
                    batchItems.Add(obj);
                }
            }

            var payloadObj = new System.Text.Json.Nodes.JsonObject
            {
                ["value"] = new System.Text.Json.Nodes.JsonArray(batchItems.ToArray())
            };

            var payloadJson = payloadObj.ToJsonString();

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Upload failed: {response.StatusCode} - {content}");
            }

            // Check for individual item failures in the batch response
            using var responseDoc = JsonDocument.Parse(content);
            if (responseDoc.RootElement.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == JsonValueKind.Array)
            {
                var errors = new List<string>();
                foreach (var item in valueElement.EnumerateArray())
                {
                    if (item.TryGetProperty("status", out var statusElement) && !statusElement.GetBoolean())
                    {
                        var key = item.TryGetProperty("key", out var keyElement) ? keyElement.GetString() : "unknown";
                        var errorMsg = item.TryGetProperty("errorMessage", out var errorElement) ? errorElement.GetString() : "Unknown error";
                        errors.Add($"Document '{key}' failed: {errorMsg}");
                    }
                }

                if (errors.Any())
                {
                    var errorSummary = string.Join("; ", errors.Take(5));
                    if (errors.Count > 5) errorSummary += $" ... and {errors.Count - 5} more errors.";
                    throw new Exception($"Partial upload failure. {errors.Count} documents failed. First few errors: {errorSummary}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading documents to index {IndexName}", indexName);
            throw;
        }
    }
}
