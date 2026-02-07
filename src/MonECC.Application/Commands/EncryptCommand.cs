using MonECC.Domain.Interfaces;
using MonECC.Domain.Model;
using System.Text;

namespace MonECC.Application.Commands;

public class EncryptCommand(IFileSystem fileSystem, ICryptoProvider crypto)
{
    private readonly Curve _curve = new(35, 3, 101);

    /// <summary>
    /// Chiffre un message avec la clé privée locale et la clé publique du destinataire.
    /// </summary>
    public async Task<string> ExecuteAsync(string privateKeyFile, string targetPublicKeyFile, string plainText)
    {
        // 1. Chargement des clés depuis les fichiers
        long myK = await LoadPrivateKey(privateKeyFile);
        Point targetQ = await LoadPublicKey(targetPublicKeyFile);

        // 2. Calcul du secret partagé S = k * Q_cible (ECDH)
        Point sharedSecretPoint = _curve.Multiply(targetQ, myK);

        if (sharedSecretPoint.IsInfinity)
            throw new Exception("Le secret partagé est le point à l'infini (clé invalide).");

        // 3. Dérivation de la clé AES : SHA256 sur la coordonnée X du secret
        byte[] secretBytes = Encoding.UTF8.GetBytes(sharedSecretPoint.X.ToString());
        byte[] hash = crypto.ComputeSha256(secretBytes);

        // IV = 16 premiers octets du hash, Clé AES = 16 derniers octets
        byte[] iv = hash[..16];
        byte[] key = hash[16..];

        // 4. Chiffrement AES-128/CBC/PKCS7 et retour en Base64
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherBytes = crypto.AesEncrypt(plainBytes, key, iv);

        return Convert.ToBase64String(cipherBytes);
    }

    /// <summary>
    /// Lit et décode la clé privée (base64 -> k) depuis un fichier .priv
    /// </summary>
    private async Task<long> LoadPrivateKey(string path)
    {
        string content = await fileSystem.ReadAllTextAsync(path);
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (!lines[0].Contains("begin monECC private")) throw new FormatException("Header clé privée invalide");
        return long.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(lines[1].Trim())));
    }

    /// <summary>
    /// Lit et décode la clé publique (base64 -> "Qx;Qy") depuis un fichier .pub
    /// </summary>
    private async Task<Point> LoadPublicKey(string path)
    {
        string content = await fileSystem.ReadAllTextAsync(path);
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (!lines[0].Contains("begin monECC public")) throw new FormatException("Header clé publique invalide");
        var parts = Encoding.UTF8.GetString(Convert.FromBase64String(lines[1].Trim())).Split(';');
        return new Point(long.Parse(parts[0]), long.Parse(parts[1]));
    }
}
