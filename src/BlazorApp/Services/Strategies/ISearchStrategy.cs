namespace BlazorApp.Services.Strategies;

public interface ISearchStrategy
{
    /// <summary>
    /// The type of search this strategy implements (e.g., "Semantic", "Vector").
    /// </summary>
    string StrategyType { get; }

    /// <summary>
    /// Determines if this strategy handles the given index name.
    /// </summary>
    bool CanHandle(string indexName);

    /// <summary>
    /// Executes the search and chat flow.
    /// </summary>
    Task<string> ExecuteSearchAndChatAsync(string indexName, string userPrompt);
}
