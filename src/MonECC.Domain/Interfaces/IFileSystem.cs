namespace MonECC.Domain.Interfaces;

public interface IFileSystem
{
    Task WriteAllTextAsync(string path, string content);
    Task<string> ReadAllTextAsync(string path);
    bool Exists(string path);
}