namespace BlazorApp.Models;

/// <summary>
/// Represents the result of an index operation.
/// </summary>
public class IndexOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? IndexName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the result of index JSON validation.
/// </summary>
public class IndexValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}
