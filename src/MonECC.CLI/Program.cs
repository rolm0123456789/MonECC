using MonECC.Application.Commands;
using MonECC.CLI;
using MonECC.Infrastructure.IO;
using MonECC.Infrastructure.Security;

// --- Composition Root : injection manuelle des dépendances ---
var fileSystem = new FileSystemAdapter();
var cryptoProvider = new SysCryptoProvider();

var keyGenCmd = new KeyGenCommand(fileSystem);
var encryptCmd = new EncryptCommand(fileSystem, cryptoProvider);
var decryptCmd = new DecryptCommand(fileSystem, cryptoProvider);

// --- Parsing des arguments CLI ---
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
            await keyGenCmd.ExecuteAsync(options.KeyName, options.KeySize);
            break;

        case "crypt":
            if (options.PositionalArgs.Count < 1) throw new ArgumentException("Clé publique manquante.");

            string pubKeyFile = options.PositionalArgs[0];
            string plainText = await GetContentAsync(fileSystem, options.InputFile, options.PositionalArgs, 1);

            string cipherText = await encryptCmd.ExecuteAsync("monECC.priv", pubKeyFile, plainText);

            await HandleOutputAsync(fileSystem, cipherText, options.OutputFile, "Message chiffré (Base64)");
            break;

        case "decrypt":
            if (options.PositionalArgs.Count < 1) throw new ArgumentException("Clé privée manquante.");

            string privKeyFile = options.PositionalArgs[0];
            string cipherInput = await GetContentAsync(fileSystem, options.InputFile, options.PositionalArgs, 1);

            string decryptedText = await decryptCmd.ExecuteAsync(privKeyFile, "monECC.pub", cipherInput.Trim());

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

/// <summary>
/// Récupère le contenu à traiter : soit depuis un fichier (-i), soit depuis un argument positionnel.
/// </summary>
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

/// <summary>
/// Écrit le résultat dans un fichier (-o) ou l'affiche sur la console.
/// </summary>
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
    Console.WriteLine("MonECC - Outil de chiffrement ECC/AES");
    Console.WriteLine();
    Console.WriteLine("Syntaxe :");
    Console.WriteLine("  monECC <commande> [<clé>] [<texte>] [switchs]");
    Console.WriteLine();
    Console.WriteLine("Commande :");
    Console.WriteLine("  keygen  : Génère une paire de clé");
    Console.WriteLine("  crypt   : Chiffre <texte> pour la clé publique <clé>");
    Console.WriteLine("  decrypt : Déchiffre <texte> pour la clé privée <clé>");
    Console.WriteLine("  help    : Affiche ce manuel");
    Console.WriteLine();
    Console.WriteLine("Clé :");
    Console.WriteLine("  Un fichier contenant une clé publique (crypt) ou privée (decrypt)");
    Console.WriteLine();
    Console.WriteLine("Texte :");
    Console.WriteLine("  Une phrase en clair (crypt) ou chiffrée en Base64 (decrypt)");
    Console.WriteLine();
    Console.WriteLine("Switchs :");
    Console.WriteLine("  -f <file>  Nom des clés générées (défaut: monECC.pub / monECC.priv)");
    Console.WriteLine("  -s <size>  Plage d'aléa de la clé (défaut: 1000)");
    Console.WriteLine("  -i <file>  Fichier d'entrée (texte en clair ou chiffré)");
    Console.WriteLine("  -o <file>  Fichier de sortie (au lieu d'afficher)");
}