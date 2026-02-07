using MonECC.Application.Commands;
using MonECC.CLI; // Nécessaire pour ArgParser
using MonECC.Infrastructure.IO;
using MonECC.Infrastructure.Security;

// --- Composition Root ---
var fileSystem = new FileSystemAdapter();
var cryptoProvider = new SysCryptoProvider();

var keyGenCmd = new KeyGenCommand(fileSystem);
var encryptCmd = new EncryptCommand(fileSystem, cryptoProvider);
var decryptCmd = new DecryptCommand(fileSystem, cryptoProvider);

// --- Parsing ---
// On délègue toute la complexité au parser
var options = ArgParser.Parse(args);

if (options.ShowHelp || string.IsNullOrEmpty(options.Command))
{
    ShowHelp();
    return;
}

try
{
    switch (options.Command)
    {
        case "keygen":
            // Utilise directement les valeurs typées
            await keyGenCmd.ExecuteAsync(options.KeyName, options.KeySize);
            break;

        case "crypt":
            // Validation
            if (options.PositionalArgs.Count < 1) throw new ArgumentException("Clé publique manquante.");

            string pubKeyFile = options.PositionalArgs[0];

            // Logique Input (-i ou Argument texte)
            string plainText = await GetContentAsync(fileSystem, options.InputFile, options.PositionalArgs, 1);
            string myPrivKeyFile = "monECC.priv"; // Défaut TP

            string cipherText = await encryptCmd.ExecuteAsync(myPrivKeyFile, pubKeyFile, plainText);

            // Logique Output (-o)
            await HandleOutputAsync(fileSystem, cipherText, options.OutputFile, "Message chiffré (Base64)");
            break;

        case "decrypt":
            if (options.PositionalArgs.Count < 1) throw new ArgumentException("Clé privée manquante.");

            string privKeyFile = options.PositionalArgs[0];

            // Logique Input (-i ou Argument texte)
            string cipherInput = await GetContentAsync(fileSystem, options.InputFile, options.PositionalArgs, 1);
            string senderPubKeyFile = "monECC.pub"; // Défaut TP

            cipherInput = cipherInput.Trim(); // Nettoyage

            string decryptedText = await decryptCmd.ExecuteAsync(privKeyFile, senderPubKeyFile, cipherInput);

            // Logique Output (-o)
            await HandleOutputAsync(fileSystem, decryptedText, options.OutputFile, "Message déchiffré");
            break;

        default:
            Console.WriteLine($"Commande inconnue : {options.Command}");
            ShowHelp();
            break;
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERREUR : {ex.Message}");
    Console.ResetColor();
}

// --- Helpers Simplifiés ---

static async Task<string> GetContentAsync(FileSystemAdapter fs, string? inputFile, List<string> positionalArgs, int argIndex)
{
    if (!string.IsNullOrEmpty(inputFile))
    {
        Console.WriteLine($"Lecture fichier : {inputFile}");
        return await fs.ReadAllTextAsync(inputFile);
    }

    if (argIndex < positionalArgs.Count)
    {
        return positionalArgs[argIndex];
    }

    throw new ArgumentException("Aucun contenu fourni. Utilisez un argument texte ou -i <fichier>.");
}

static async Task HandleOutputAsync(FileSystemAdapter fs, string content, string? outputFile, string label)
{
    if (!string.IsNullOrEmpty(outputFile))
    {
        await fs.WriteAllTextAsync(outputFile, content);
        Console.WriteLine($"Résultat sauvegardé dans : {outputFile}");
    }
    else
    {
        Console.WriteLine($"{label} :\n{content}");
    }
}

static void ShowHelp()
{
    Console.WriteLine("--- MonECC - Outil de chiffrement ECC/AES ---");
    Console.WriteLine("Usage:");
    Console.WriteLine("  monECC keygen [-f filename] [-s size]");
    Console.WriteLine("  monECC crypt <pubKey> [<msg>] [-i input] [-o output]");
    Console.WriteLine("  monECC decrypt <privKey> [<cipher>] [-i input] [-o output]");
}