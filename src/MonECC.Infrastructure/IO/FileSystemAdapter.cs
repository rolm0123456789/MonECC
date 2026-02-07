using MonECC.Domain.Interfaces;

namespace MonECC.Infrastructure.IO;

public class FileSystemAdapter : IFileSystem
{
    public async Task WriteAllTextAsync(string path, string content)
    {
        // En .NET moderne, l'UTF8 sans BOM est le défaut, c'est ce qu'on veut.
        await File.WriteAllTextAsync(path, content);
    }

    public async Task<string> ReadAllTextAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Le fichier requis est introuvable : {path}");
        }
        return await File.ReadAllTextAsync(path);
    }

    public bool Exists(string path) => File.Exists(path);
}