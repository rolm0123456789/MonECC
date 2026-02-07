using MonECC.Application.Commands;
using MonECC.Domain.Interfaces;
using MonECC.Infrastructure.Security;

namespace MonECC.Tests;

public class CommandTests
{
    // --- Mock du FileSystem (Pour ne pas écrire sur le disque) ---
    public class FakeFileSystem : IFileSystem
    {
        public Dictionary<string, string> Files { get; } = new();

        public Task WriteAllTextAsync(string path, string content)
        {
            Files[path] = content;
            return Task.CompletedTask;
        }

        public Task<string> ReadAllTextAsync(string path)
        {
            if (!Files.ContainsKey(path)) throw new FileNotFoundException();
            return Task.FromResult(Files[path]);
        }

        public bool Exists(string path) => Files.ContainsKey(path);
    }

    [Fact]
    public async Task FullFlow_KeyGen_Encrypt_Decrypt_ShouldWork()
    {
        // 1. Setup (Infrastructure simulée)
        var fakeFs = new FakeFileSystem();
        var crypto = new SysCryptoProvider(); // On utilise la vraie crypto (AES/SHA)

        var keyGen = new KeyGenCommand(fakeFs);
        var encrypt = new EncryptCommand(fakeFs, crypto);
        var decrypt = new DecryptCommand(fakeFs, crypto);

        // 2. Génération des clés
        await keyGen.ExecuteAsync("testKey");

        // Vérification que les fichiers virtuels existent
        Assert.True(fakeFs.Exists("testKey.priv"));
        Assert.True(fakeFs.Exists("testKey.pub"));

        // 3. Chiffrement
        string messageSecret = "Architecture Logiciel 20/20";
        // On chiffre pour soi-même (testKey.pub) avec sa propre clé (testKey.priv)
        // Note: Dans le TP, Encrypt utilise "monECC.priv" par défaut pour dériver le secret,
        // ici on simule que "testKey.priv" est la clé de l'utilisateur.

        // Hack pour le test : EncryptCommand charge "monECC.priv" en dur dans le code que j'ai donné avant ?
        // Ah ! Dans le code précédent, on passait "myPrivKeyFile" en argument à ExecuteAsync. 
        // C'est parfait, l'architecture est testable !

        string cipherBase64 = await encrypt.ExecuteAsync("testKey.priv", "testKey.pub", messageSecret);

        Assert.False(string.IsNullOrEmpty(cipherBase64));
        Assert.NotEqual(messageSecret, cipherBase64);

        // 4. Déchiffrement
        string decryptedMessage = await decrypt.ExecuteAsync("testKey.priv", "testKey.pub", cipherBase64);

        // 5. Validation finale
        Assert.Equal(messageSecret, decryptedMessage);
    }
}
