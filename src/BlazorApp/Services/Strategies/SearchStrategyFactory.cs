namespace BlazorApp.Services.Strategies;

public class SearchStrategyFactory
{
    private readonly IEnumerable<ISearchStrategy> _strategies;

    public SearchStrategyFactory(IEnumerable<ISearchStrategy> strategies)
    {
        _strategies = strategies;
    }

    public ISearchStrategy GetStrategy(string indexName)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(indexName));
        
        if (strategy == null)
        {
            // Fallback or throw? 
            // For now, let's throw to be explicit, or return a default "Simple Search" strategy if we had one.
            throw new InvalidOperationException($"No search strategy found for index '{indexName}'. Ensure the index name contains 'semantic' for Semantic Search.");
        }

        return strategy;
    }
}
