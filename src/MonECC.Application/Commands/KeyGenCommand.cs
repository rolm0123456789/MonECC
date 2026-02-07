using MonECC.Domain.Interfaces;
using MonECC.Domain.Model;
using System.Text;

namespace MonECC.Application.Commands;

public class KeyGenCommand(IFileSystem fileSystem)
{
    // Courbe du TP : y² = x³ + 35x + 3 (mod 101)
    private readonly Curve _curve = new(35, 3, 101);
    // Point générateur G(2, 9) sur la courbe
    private readonly Point _G = new(2, 9);

    /// <summary>
    /// Génère une paire de clés (privée k, publique Q = k*G) et les sauvegarde.
    /// </summary>
    public async Task ExecuteAsync(string outputName = "monECC", int maxKeySize = 1000)
    {
        var random = new Random();
        long k;
        Point Q;
        int attempts = 0;

        Console.WriteLine($"Génération de clés (Max k={maxKeySize})...");

        // Tire k aléatoirement et calcule Q = k*G
        // Boucle tant que Q est invalide (point à l'infini ou Y=0)
        do
        {
            attempts++;
            k = random.Next(1, maxKeySize + 1);
            Q = _curve.Multiply(_G, k);
        }
        while ((Q.IsInfinity || Q.Y == 0) && attempts < 1000);

        if (Q.IsInfinity || Q.Y == 0)
            throw new Exception("Impossible de générer une clé valide.");

        await SavePrivateKeyAsync(k, $"{outputName}.priv");
        await SavePublicKeyAsync(Q, $"{outputName}.pub");
        Console.WriteLine($"Clés générées : {outputName}.priv et {outputName}.pub");
        Console.WriteLine($"Clé privée k : {k}");
        Console.WriteLine($"Clé publique Q : {Q}");
    }

    /// <summary>
    /// Sauvegarde la clé privée au format : header / base64(k) / footer
    /// </summary>
    private async Task SavePrivateKeyAsync(long k, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---begin monECC private key---");
        sb.AppendLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(k.ToString())));
        sb.Append("---end monECC key---");
        await fileSystem.WriteAllTextAsync(path, sb.ToString());
    }

    /// <summary>
    /// Sauvegarde la clé publique au format : header / base64("Qx;Qy") / footer
    /// </summary>
    private async Task SavePublicKeyAsync(Point q, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---begin monECC public key---");
        sb.AppendLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(q.ToString())));
        sb.Append("---end monECC key---");
        await fileSystem.WriteAllTextAsync(path, sb.ToString());
    }
}