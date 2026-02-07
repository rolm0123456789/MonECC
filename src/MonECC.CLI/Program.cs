using MonECC.Infrastructure.IO;
using MonECC.Application.Commands;
using MonECC.Infrastructure.Security;

// --- Composition Root (Injection de dépendances) ---
var fileSystem = new FileSystemAdapter();
var cryptoProvider = new SysCryptoProvider();

var keyGenCmd = new KeyGenCommand(fileSystem);
var encryptCmd = new EncryptCommand(fileSystem, cryptoProvider);
var decryptCmd = new DecryptCommand(fileSystem, cryptoProvider);

// --- Parsing Arguments ---
if (args.Length == 0 || args.Contains("help") || args.Contains("-h"))
{
    ShowHelp();
    return;
}

string command = args[0].ToLower();

try
{
    switch (command)
    {
        case "keygen":
            // Gestion du switch optionnel -f
            string outputName = GetSwitchValue(args, "-f") ?? "monECC";
            await keyGenCmd.ExecuteAsync(outputName);
            break;

        case "crypt":
            // monECC crypt <clé_publique> <texte> [-o output]
            if (args.Length < 3) throw new ArgumentException("Arguments manquants. Usage: monECC crypt <public_key_file> <texte>");

            string pubKeyFile = args[1];
            string plainText = args[2];
            // Le TP ne précise pas où est la clé privée de l'expéditeur pour crypter...
            // Pour le TP on va supposer qu'on génère une clé éphémère ou qu'on utilise "monECC.priv" par défaut 
            // car ECC nécessite MA clé privée + SA clé publique pour dériver le secret.
            // Hypothèse "Major" : On cherche une clé privée locale par défaut.
            string myPrivKeyFile = "monECC.priv";

            string cipherText = await encryptCmd.ExecuteAsync(myPrivKeyFile, pubKeyFile, plainText);

            Console.WriteLine($"Message chiffré (Base64) :\n{cipherText}");
            break;

        case "decrypt":
            // monECC decrypt <clé_privée> <texte_chiffré>
            // Note: Comme vu précédemment, il manque la clé publique de l'émetteur dans les specs du TP.
            // On va supposer qu'on la passe ou qu'elle est "monECC.pub" par défaut pour tester.
            if (args.Length < 3) throw new ArgumentException("Arguments manquants. Usage: monECC decrypt <private_key_file> <texte_chiffré>");

            string privKeyFile = args[1];
            string cipherB64 = args[2];
            string senderPubKeyFile = "monECC.pub"; // Valeur par défaut pour le TP

            string decryptedText = await decryptCmd.ExecuteAsync(privKeyFile, senderPubKeyFile, cipherB64);

            Console.WriteLine($"Message déchiffré :\n{decryptedText}");
            break;

        default:
            Console.WriteLine($"Commande inconnue : {command}");
            ShowHelp();
            break;
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERREUR : {ex.Message}");
    Console.ResetColor();
    // En mode debug on peut afficher la stacktrace, mais en prod on évite.
    // Console.WriteLine(ex.StackTrace); 
}

// --- Helpers CLI ---

static string? GetSwitchValue(string[] args, string switchName)
{
    int index = Array.IndexOf(args, switchName);
    if (index != -1 && index + 1 < args.Length)
    {
        return args[index + 1];
    }
    return null;
}

static void ShowHelp()
{
    Console.WriteLine("--- MonECC - Outil de chiffrement ECC/AES ---");
    Console.WriteLine("Usage:");
    Console.WriteLine("  monECC keygen [-f filename]           : Génère une paire de clés.");
    Console.WriteLine("  monECC crypt <pubKey> <msg>           : Chiffre un message.");
    Console.WriteLine("  monECC decrypt <privKey> <cipher>     : Déchiffre un message.");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -f <file> : Préfixe des fichiers clés (défaut: monECC)");
}
