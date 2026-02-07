using MonECC.Domain.Interfaces;
using System.Security.Cryptography;

namespace MonECC.Infrastructure.Security;

/// <summary>
/// Implémente les primitives cryptographiques standards via System.Security.Cryptography.
/// </summary>
 public class SysCryptoProvider : ICryptoProvider
{
    /// <summary>
    /// Calcule le hash SHA256 d'une donnée brute (one-shot .NET).
    /// </summary>
    public byte[] ComputeSha256(byte[] data) => SHA256.HashData(data);

    /// <summary>
    /// Chiffre en AES-128 / CBC / PKCS7.
    /// </summary>
    public byte[] AesEncrypt(byte[] plainText, byte[] key, byte[] iv)
    {
        using Aes aes = Aes.Create();
        ConfigureAes(aes, key, iv);
        using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        return encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
    }

    /// <summary>
    /// Déchiffre en AES-128 / CBC / PKCS7.
    /// </summary>
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
            throw new CryptographicException("Padding invalide ou mauvaise clé.");
        }
    }

    /// <summary>
    /// Configuration commune AES : 128 bits, CBC, PKCS7 (exigences du TP).
    /// </summary>
    private static void ConfigureAes(Aes aes, byte[] key, byte[] iv)
    {
        aes.KeySize = 128;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;
    }
}