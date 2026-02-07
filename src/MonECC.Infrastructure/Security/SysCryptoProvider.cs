using MonECC.Domain.Interfaces;
using System.Security.Cryptography;

namespace MonECC.Infrastructure.Security;

public class SysCryptoProvider : ICryptoProvider
{
    public byte[] ComputeSha256(byte[] data)
    {
        // Méthode optimisée "One-shot" de .NET (zéro allocation inutile)
        return SHA256.HashData(data);
    }

    public byte[] AesEncrypt(byte[] plainText, byte[] key, byte[] iv)
    {
        using Aes aes = Aes.Create();
        ConfigureAes(aes, key, iv);

        // Crée le transformateur
        using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        // TransformFinalBlock est suffisant ici car on chiffre tout le message d'un coup
        // (Pour des fichiers de plusieurs Go, on utiliserait des Streams, mais ici c'est du texte court).
        return encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
    }

    public byte[] AesDecrypt(byte[] cipherText, byte[] key, byte[] iv)
    {
        using Aes aes = Aes.Create();
        ConfigureAes(aes, key, iv);

        using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        try
        {
            return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        }
        catch (CryptographicException)
        {
            // On relance une exception plus claire pour l'utilisateur
            throw new CryptographicException("Padding invalide ou mauvaise clé. Impossible de déchiffrer.");
        }
    }

    // Configuration centralisée pour éviter les erreurs de copier-coller entre Encrypt/Decrypt
    private void ConfigureAes(Aes aes, byte[] key, byte[] iv)
    {
        aes.KeySize = 128; // Le TP implique 128 bits (16 bytes de clé extraits du SHA256)
        aes.BlockSize = 128; // AES est toujours 128 bits
        aes.Mode = CipherMode.CBC; // Exigence TP
        aes.Padding = PaddingMode.PKCS7; // Exigence TP

        aes.Key = key;
        aes.IV = iv;
    }
}