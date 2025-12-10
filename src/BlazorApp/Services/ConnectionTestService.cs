using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents.Indexes;
using BlazorApp.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace BlazorApp.Services;

public class ConnectionTestService
{
    private readonly AzureSettings _settings;
    private readonly DefaultAzureCredential _credential;

    public ConnectionTestService(IOptions<AzureSettings> settings)
    {
        _settings = settings.Value;
        _credential = new DefaultAzureCredential();
    }

    public async Task<ServiceStatus> TestGpt4ConnectionAsync()
    {
        try
        {
            var client = new AzureOpenAIClient(
                new Uri(_settings.AzureAI.Endpoint),
                _credential);

            // Simple test to verify we can access the deployment
            var chatClient = client.GetChatClient(_settings.AzureAI.GptDeploymentName);
            
            // Try a minimal completion to test connectivity
            var result = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage("You are a helpful assistant."),
                new UserChatMessage("Hi")
            ],
            new ChatCompletionOptions
            {
                MaxOutputTokenCount = 5
            });

            return new ServiceStatus
            {
                IsConnected = true,
                Message = "Successfully connected to GPT-4.1",
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ServiceStatus
            {
                IsConnected = false,
                Message = $"Failed to connect: {ex.Message}",
                LastChecked = DateTime.UtcNow
            };
        }
    }

    public async Task<ServiceStatus> TestEmbeddingConnectionAsync()
    {
        try
        {
            var client = new AzureOpenAIClient(
                new Uri(_settings.AzureAI.Endpoint),
                _credential);

            var embeddingClient = client.GetEmbeddingClient(_settings.AzureAI.EmbeddingDeploymentName);
            
            // Try a minimal embedding to test connectivity
            var result = await embeddingClient.GenerateEmbeddingAsync("test");

            return new ServiceStatus
            {
                IsConnected = true,
                Message = "Successfully connected to text-embedding-3-large",
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ServiceStatus
            {
                IsConnected = false,
                Message = $"Failed to connect: {ex.Message}",
                LastChecked = DateTime.UtcNow
            };
        }
    }

    public async Task<ServiceStatus> TestSearchConnectionAsync()
    {
        try
        {
            var searchClient = new SearchIndexClient(
                new Uri(_settings.AzureSearch.Endpoint),
                _credential);

            // Try to list indexes to verify connectivity
            var indexNames = new List<string>();
            await foreach (var indexName in searchClient.GetIndexNamesAsync())
            {
                indexNames.Add(indexName);
            }

            return new ServiceStatus
            {
                IsConnected = true,
                Message = $"Successfully connected to Azure AI Search ({indexNames.Count} indexes)",
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ServiceStatus
            {
                IsConnected = false,
                Message = $"Failed to connect: {ex.Message}",
                LastChecked = DateTime.UtcNow
            };
        }
    }
}

public class ServiceStatus
{
    public bool IsConnected { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; }
}
