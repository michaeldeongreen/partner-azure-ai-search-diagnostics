namespace BlazorApp.Models;

public class AzureSettings
{
    public AzureAISettings AzureAI { get; set; } = new();
    public AzureSearchSettings AzureSearch { get; set; } = new();
}

public class AzureAISettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string GptDeploymentName { get; set; } = string.Empty;
    public string EmbeddingDeploymentName { get; set; } = string.Empty;
}

public class AzureSearchSettings
{
    public string Endpoint { get; set; } = string.Empty;
}
