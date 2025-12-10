using System.Collections.ObjectModel;

namespace BlazorApp.Services.Embeddings;

public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text);
}
