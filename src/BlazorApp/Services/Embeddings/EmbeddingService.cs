using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using BlazorApp.Models;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace BlazorApp.Services.Embeddings;

public class EmbeddingService : IEmbeddingService
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<EmbeddingService> _logger;
    private const int MaxRetries = 3;
    private const int BaseDelayMs = 1000;

    public EmbeddingService(IOptions<AzureSettings> settings, ILogger<EmbeddingService> logger)
    {
        _logger = logger;
        var azureSettings = settings.Value.AzureAI;

        if (string.IsNullOrWhiteSpace(azureSettings.Endpoint))
            throw new ArgumentException("Azure OpenAI Endpoint is missing");
        
        if (string.IsNullOrWhiteSpace(azureSettings.EmbeddingDeploymentName))
            throw new ArgumentException("Azure OpenAI Embedding Deployment Name is missing");

        _deploymentName = azureSettings.EmbeddingDeploymentName;
        
        var credential = new DefaultAzureCredential();
        _client = new AzureOpenAIClient(new Uri(azureSettings.Endpoint), credential);
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ReadOnlyMemory<float>.Empty;
        }

        var embeddingClient = _client.GetEmbeddingClient(_deploymentName);
        
        int attempt = 0;
        while (true)
        {
            try
            {
                attempt++;
                var options = new EmbeddingGenerationOptions { Dimensions = 1536 };
                OpenAIEmbedding embedding = await embeddingClient.GenerateEmbeddingAsync(text, options);
                return embedding.ToFloats();
            }
            catch (RequestFailedException ex) when (ex.Status == 429 && attempt <= MaxRetries)
            {
                // Rate limit exceeded
                _logger.LogWarning($"Rate limit hit for embedding generation. Attempt {attempt}/{MaxRetries}. Retrying...");
                await Task.Delay(BaseDelayMs * (int)Math.Pow(2, attempt)); // Exponential backoff
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding");
                throw;
            }
        }
    }
}
