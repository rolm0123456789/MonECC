using MonECC.Domain.Interfaces;
using MonECC.Domain.Model;
using System.Text;

namespace MonECC.Application.Commands;

public class EncryptCommand(IFileSystem fileSystem, ICryptoProvider crypto)
{
    private readonly Curve _curve = new(35, 3, 101);

    /// <summary>
    /// Chiffre un message.
    /// </summary>
    /// <param name="privateKeyFile">Chemin vers MA clé privée</param>
    /// <param name="targetPublicKeyFile">Chemin vers la clé publique du DESTINATAIRE</param>
    /// <param name="plainText">Texte à chiffrer</param>
    public async Task<string> ExecuteAsync(string privateKeyFile, string targetPublicKeyFile, string plainText)
    {
        // 1. Chargement des clés
        long myK = await LoadPrivateKey(privateKeyFile);
        Point targetQ = await LoadPublicKey(targetPublicKeyFile);

        // 2. Calcul du secret partagé S = k * Q_cible
        Point sharedSecretPoint = _curve.Multiply(targetQ, myK);

        if (sharedSecretPoint.IsInfinity)
            throw new Exception("Erreur critique : Le secret partagé est le point à l'infini (clé invalide ?).");

        // 3. Dérivation de la clé AES et IV via SHA256
        // Le TP Python suggère de hasher la coordonnée X : sha256(S.x)
        byte[] secretBytes = Encoding.UTF8.GetBytes(sharedSecretPoint.X.ToString());
        byte[] hash = crypto.ComputeSha256(secretBytes);

        // IV = 16 premiers chars (bytes), Key = 16 derniers
        byte[] iv = hash[..16];
        byte[] key = hash[16..];

        // 4. Chiffrement AES
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherBytes = crypto.AesEncrypt(plainBytes, key, iv);

        // Retour en Hex ou Base64 ? Le TP affiche une string byte[] dans l'exemple, 
        // mais pour le CLI on retourne souvent du Base64 ou Hex. 
        // Le TP print "Le texte chiffré est b'...'" donc Hex ou Base64 est acceptable.
        // Allons vers Base64 pour la propreté.
        return Convert.ToBase64String(cipherBytes);
    }

    // --- Helpers de parsing (Logique Application car liée au format de fichier spécifié) ---

    private async Task<long> LoadPrivateKey(string path)
    {
        string content = await fileSystem.ReadAllTextAsync(path);
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (!lines[0].Contains("begin monECC private")) throw new FormatException("Header clé privée invalide");

        string b64 = lines[1].Trim();
        string kStr = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        return long.Parse(kStr);
    }

    private async Task<Point> LoadPublicKey(string path)
    {
        string content = await fileSystem.ReadAllTextAsync(path);
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (!lines[0].Contains("begin monECC public")) throw new FormatException("Header clé publique invalide");

        string b64 = lines[1].Trim();
        string coords = Encoding.UTF8.GetString(Convert.FromBase64String(b64));

        var parts = coords.Split(';');
        return new Point(long.Parse(parts[0]), long.Parse(parts[1]));
    }
}
