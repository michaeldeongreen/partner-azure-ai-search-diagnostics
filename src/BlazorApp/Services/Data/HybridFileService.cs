namespace BlazorApp.Services.Data;

public class HybridFileService : IHybridFileService
{
    private readonly string _dataDirectory;
    private readonly ILogger<HybridFileService> _logger;

    public HybridFileService(IWebHostEnvironment env, ILogger<HybridFileService> logger)
    {
        _logger = logger;
        // Assuming the data folder is at the root of the repo, relative to the project
        // Adjust path logic as needed. Here we assume it's in a known location relative to ContentRoot
        // Or we can use an absolute path if we know it.
        // Given the workspace structure: <repo_root>/data/hybrid
        // And the app runs from: <repo_root>/src/BlazorApp
        
        // We'll try to find the data folder by going up two levels from ContentRoot
        var rootPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", ".."));
        _dataDirectory = Path.Combine(rootPath, "data", "hybrid");
    }

    public Task<List<string>> ListFilesAsync()
    {
        if (!Directory.Exists(_dataDirectory))
        {
            return Task.FromResult(new List<string>());
        }

        var files = Directory.GetFiles(_dataDirectory, "*.json")
                             .Select(Path.GetFileName)
                             .Where(f => f != null)
                             .Cast<string>()
                             .OrderBy(f => f)
                             .ToList();
        
        return Task.FromResult(files);
    }

    public async Task<string> ReadFileAsync(string fileName)
    {
        var path = Path.Combine(_dataDirectory, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found: {fileName}");
        }

        return await File.ReadAllTextAsync(path);
    }

    public async Task SaveFileAsync(string fileName, string content)
    {
        var path = Path.Combine(_dataDirectory, fileName);
        await File.WriteAllTextAsync(path, content);
    }
}
