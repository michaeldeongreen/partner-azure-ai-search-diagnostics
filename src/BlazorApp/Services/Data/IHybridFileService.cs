namespace BlazorApp.Services.Data;

public interface IHybridFileService
{
    Task<List<string>> ListFilesAsync();
    Task<string> ReadFileAsync(string fileName);
    Task SaveFileAsync(string fileName, string content);
}
