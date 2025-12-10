using BlazorApp.Models;

namespace BlazorApp.Services;

/// <summary>
/// Interface for Azure AI Search index operations.
/// Follows Interface Segregation Principle - focused on index management only.
/// </summary>
public interface IAzureSearchIndexService
{
    /// <summary>
    /// Creates a new search index from JSON definition.
    /// </summary>
    /// <param name="indexJson">JSON string containing the index definition</param>
    /// <returns>Result containing success status and message</returns>
    Task<IndexOperationResult> CreateIndexAsync(string indexJson);

    /// <summary>
    /// Lists all available indexes in the search service.
    /// </summary>
    /// <returns>List of index names</returns>
    Task<List<string>> ListIndexesAsync();

    /// <summary>
    /// Uploads a batch of documents to the specified index.
    /// </summary>
    /// <param name="indexName">Target index name</param>
    /// <param name="jsonDocuments">List of JSON strings representing the documents</param>
    Task UploadDocumentsAsync(string indexName, List<string> jsonDocuments);

    /// <summary>
    /// Validates the index JSON format.
    /// </summary>
    /// <param name="indexJson">JSON string to validate</param>
    /// <returns>Result indicating if JSON is valid</returns>
    IndexValidationResult ValidateIndexJson(string indexJson);
}
