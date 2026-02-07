using MonECC.Domain.Interfaces;
using MonECC.Domain.Model;
using System.Text;

namespace MonECC.Application.Commands;

public class KeyGenCommand(IFileSystem fileSystem)
{
    private readonly Curve _curve = new(35, 3, 101);
    private readonly Point _G = new(2, 9);

    // Ajout du paramètre maxKeySize
    public async Task ExecuteAsync(string outputName = "monECC", int maxKeySize = 1000)
    {
        var random = new Random();
        long k;
        Point Q;
        int attempts = 0;

        Console.WriteLine($"Génération de clés (Max k={maxKeySize})...");

        do
        {
            attempts++;
            // Utilisation de la taille dynamique demandée par le switch -s
            k = random.Next(1, maxKeySize + 1);
            Q = _curve.Multiply(_G, k);
        }
        while ((Q.IsInfinity || Q.Y == 0) && attempts < 1000); // Augmenter les essais si la plage est grande

        if (Q.IsInfinity || Q.Y == 0)
            throw new Exception("Impossible de générer une clé valide.");

        await SavePrivateKeyAsync(k, $"{outputName}.priv");
        await SavePublicKeyAsync(Q, $"{outputName}.pub");
        Console.WriteLine($"Clés générées : {outputName}.priv et {outputName}.pub");
        Console.WriteLine($"Clé privée k : {k}");
        Console.WriteLine($"Clé publique Q : {Q}");
    }

    // ... (Le reste des méthodes SavePrivateKeyAsync / SavePublicKeyAsync ne change pas)
    private async Task SavePrivateKeyAsync(long k, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---begin monECC private key---");
        string kStr = k.ToString();
        string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(kStr));
        sb.AppendLine(b64);
        sb.Append("---end monECC key---");
        await fileSystem.WriteAllTextAsync(path, sb.ToString());
    }

    private async Task SavePublicKeyAsync(Point q, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---begin monECC public key---");
        string qStr = q.ToString();
        string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(qStr));
        sb.AppendLine(b64);
        sb.Append("---end monECC key---");
        await fileSystem.WriteAllTextAsync(path, sb.ToString());
    }
}