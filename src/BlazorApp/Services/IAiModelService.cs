using OpenAI.Chat;

namespace BlazorApp.Services;

public interface IAiModelService
{
    Task<string> GetChatCompletionAsync(string systemPrompt, string userPrompt);
    Task<ChatCompletion> GetChatCompletionAsync(List<ChatMessage> messages, ChatCompletionOptions? options = null);
}
