using MonECC.Domain.Interfaces;
using MonECC.Domain.Model;
using MonECC.Infrastructure.IO;
using System.Text;

namespace MonECC.Application.Commands;

public class DecryptCommand(IFileSystem fileSystem, ICryptoProvider crypto)
{
    private readonly Curve _curve = new(35, 3, 101);

    /// <summary>
    /// Déchiffre un message.
    /// Note : Nécessite la clé publique de l'émetteur pour reconstruire le secret partagé.
    /// </summary>
    public async Task<string> ExecuteAsync(string privateKeyFile, string senderPublicKeyFile, string cipherTextBase64)
    {
        // 1. Chargement des clés
        long myK = await LoadPrivateKey(privateKeyFile);
        Point senderQ = await LoadPublicKey(senderPublicKeyFile);

        // 2. Calcul du secret partagé S = myK * SenderQ (Commutatif : k_a * Q_b == k_b * Q_a)
        Point sharedSecretPoint = _curve.Multiply(senderQ, myK);

        // 3. Dérivation Hash (Identique à l'encryption)
        byte[] secretBytes = Encoding.UTF8.GetBytes(sharedSecretPoint.X.ToString());
        byte[] hash = crypto.ComputeSha256(secretBytes);

        byte[] iv = hash[..16];
        byte[] key = hash[16..];

        // 4. Déchiffrement AES
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

    // (Duplication de code possible ici pour LoadKey -> Dans un vrai projet, on ferait un KeyLoaderService injecté)
    private async Task<long> LoadPrivateKey(string path)
    {
        /* Même implémentation que EncryptCommand - 
           Pour le "Perfect Project", on devrait extraire ça dans un service commun "KeyRepository" */
        string content = await fileSystem.ReadAllTextAsync(path);
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string b64 = lines[1].Trim();
        return long.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(b64)));
    }

    private async Task<Point> LoadPublicKey(string path)
    {
        /* Même implémentation que EncryptCommand */
        string content = await fileSystem.ReadAllTextAsync(path);
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string b64 = lines[1].Trim();
        var parts = Encoding.UTF8.GetString(Convert.FromBase64String(b64)).Split(';');
        return new Point(long.Parse(parts[0]), long.Parse(parts[1]));
    }
}