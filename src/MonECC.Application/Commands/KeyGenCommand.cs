using MonECC.Domain.Interfaces;
using MonECC.Domain.Model;
using MonECC.Infrastructure.IO;
using System.Text;

namespace MonECC.Application.Commands;

public class KeyGenCommand(IFileSystem fileSystem)
{
    // Paramètres du TP [cite: 22, 23]
    private readonly Curve _curve = new(35, 3, 101);
    private readonly Point _G = new(2, 9); // Point Générateur P

    public async Task ExecuteAsync(string outputName = "monECC")
    {
        // 1. Génération de la clé privée k (aléatoire entre 1 et 1000) [cite: 25]
        var random = new Random();
        long k = random.Next(1, 1001);

        // 2. Calcul de la clé publique Q = kP [cite: 26]
        Point Q = _curve.Multiply(_G, k);

        // 3. Sauvegarde formatée [cite: 81-98]
        await SavePrivateKeyAsync(k, $"{outputName}.priv");
        await SavePublicKeyAsync(Q, $"{outputName}.pub");

        Console.WriteLine($"Clés générées : {outputName}.priv et {outputName}.pub");
        Console.WriteLine($"Clé privée k : {k}");
        Console.WriteLine($"Clé publique Q : {Q}");
    }

    private async Task SavePrivateKeyAsync(long k, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---begin monECC private key---");
        // Encodage Base64 de la string de k
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
        // Encodage Base64 de "Qx;Qy"
        string qStr = q.ToString(); // Utilise le ToString() "X;Y" qu'on a codé
        string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(qStr));
        sb.AppendLine(b64);
        sb.Append("---end monECC key---");

        await fileSystem.WriteAllTextAsync(path, sb.ToString());
    }
}
