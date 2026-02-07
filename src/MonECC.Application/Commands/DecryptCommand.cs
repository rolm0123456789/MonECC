using MonECC.Domain.Interfaces;
using MonECC.Domain.Model;
using System.Text;

namespace MonECC.Application.Commands;

public class DecryptCommand(IFileSystem fileSystem, ICryptoProvider crypto)
{
    private readonly Curve _curve = new(35, 3, 101);

    /// <summary>
    /// Déchiffre un message avec la clé privée et la clé publique de l'émetteur.
    /// </summary>
    public async Task<string> ExecuteAsync(string privateKeyFile, string senderPublicKeyFile, string cipherTextBase64)
    {
        // 1. Chargement des clés depuis les fichiers
        long myK = await LoadPrivateKey(privateKeyFile);
        Point senderQ = await LoadPublicKey(senderPublicKeyFile);

        // 2. Reconstruction du secret partagé (commutatif : k_a * Q_b == k_b * Q_a)
        Point sharedSecretPoint = _curve.Multiply(senderQ, myK);

        // 3. Dérivation identique au chiffrement : SHA256("X;Y") puis hexdigest
        string secretString = $"{sharedSecretPoint.X};{sharedSecretPoint.Y}";
        byte[] secretBytes = Encoding.UTF8.GetBytes(secretString);
        byte[] hash = crypto.ComputeSha256(secretBytes);

        // Conversion en hexdigest
        string hexDigest = Convert.ToHexStringLower(hash); // 64 caractères hex

        // IV = 16 premiers caractères du hexdigest, Clé AES = 16 derniers caractères
        byte[] iv = Encoding.ASCII.GetBytes(hexDigest[..16]);
        byte[] key = Encoding.ASCII.GetBytes(hexDigest[^16..]);

        // 4. Déchiffrement AES-128/CBC/PKCS7
        byte[] cipherBytes = Convert.FromBase64String(cipherTextBase64);
        try
        {
            byte[] plainBytes = crypto.AesDecrypt(cipherBytes, key, iv);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            throw new Exception("Échec du déchiffrement. Mauvaises clés ou message corrompu.");
        }
    }

    /// <summary>
    /// Lit et décode la clé privée (base64 -> k) depuis un fichier .priv
    /// </summary>
    private async Task<long> LoadPrivateKey(string path)
    {
        string content = await fileSystem.ReadAllTextAsync(path);
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (!lines[0].Contains("begin monECC private")) throw new FormatException("Header clé privée invalide");
        string b64 = lines[1].Trim();
        return long.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(b64)));
    }

    /// <summary>
    /// Lit et décode la clé publique (base64 -> "Qx;Qy") depuis un fichier .pub
    /// </summary>
    private async Task<Point> LoadPublicKey(string path)
    {
        string content = await fileSystem.ReadAllTextAsync(path);
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (!lines[0].Contains("begin monECC public")) throw new FormatException("Header clé publique invalide");
        string b64 = lines[1].Trim();
        var parts = Encoding.UTF8.GetString(Convert.FromBase64String(b64)).Split(';');
        return new Point(long.Parse(parts[0]), long.Parse(parts[1]));
    }
}