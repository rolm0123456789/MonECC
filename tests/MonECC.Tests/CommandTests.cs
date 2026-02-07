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
        var fakeFs = new FakeFileSystem();
        var crypto = new SysCryptoProvider();

        var keyGen = new KeyGenCommand(fakeFs);
        var encrypt = new EncryptCommand(fakeFs, crypto);
        var decrypt = new DecryptCommand(fakeFs, crypto);

        await keyGen.ExecuteAsync("testKey");

        Assert.True(fakeFs.Exists("testKey.priv"));
        Assert.True(fakeFs.Exists("testKey.pub"));

        string messageSecret = "Architecture Logiciel 20/20";
        string cipherBase64 = await encrypt.ExecuteAsync("testKey.priv", "testKey.pub", messageSecret);

        Assert.False(string.IsNullOrEmpty(cipherBase64));
        Assert.NotEqual(messageSecret, cipherBase64);

        string decryptedMessage = await decrypt.ExecuteAsync("testKey.priv", "testKey.pub", cipherBase64);

        Assert.Equal(messageSecret, decryptedMessage);
    }
}
