using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using BlazorApp.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace BlazorApp.Services;

public class AiModelService : IAiModelService
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<AiModelService> _logger;

    public AiModelService(IOptions<AzureSettings> settings, ILogger<AiModelService> logger)
    {
        _logger = logger;
        var azureSettings = settings.Value.AzureAI;

        if (string.IsNullOrWhiteSpace(azureSettings.Endpoint))
            throw new ArgumentException("Azure OpenAI Endpoint is missing");
        
        if (string.IsNullOrWhiteSpace(azureSettings.GptDeploymentName))
            throw new ArgumentException("Azure OpenAI GPT Deployment Name is missing");

        _deploymentName = azureSettings.GptDeploymentName;
        
        var credential = new DefaultAzureCredential();
        _client = new AzureOpenAIClient(new Uri(azureSettings.Endpoint), credential);
    }

    public async Task<string> GetChatCompletionAsync(string systemPrompt, string userPrompt)
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            ChatCompletion completion = await chatClient.CompleteChatAsync(messages);

            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat completion");
            throw;
        }
    }

    public async Task<ChatCompletion> GetChatCompletionAsync(List<ChatMessage> messages, ChatCompletionOptions? options = null)
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);
            return await chatClient.CompleteChatAsync(messages, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat completion with options");
            throw;
        }
    }
}
