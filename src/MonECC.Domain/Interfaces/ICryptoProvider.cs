namespace MonECC.Domain.Interfaces;

/// <summary>
/// Abstraction pour les algorithmes standards (SHA, AES) fournis par le système.
/// </summary>
public interface ICryptoProvider
{
    /// <summary>
    /// Calcule le SHA256 d'une donnée brute.
    /// </summary>
    byte[] ComputeSha256(byte[] data);

    /// <summary>
    /// Chiffre en AES-CBC avec Padding PKCS7.
    /// </summary>
    byte[] AesEncrypt(byte[] plainText, byte[] key, byte[] iv);

    /// <summary>
    /// Déchiffre en AES-CBC avec Padding PKCS7.
    /// </summary>
    byte[] AesDecrypt(byte[] cipherText, byte[] key, byte[] iv);
}