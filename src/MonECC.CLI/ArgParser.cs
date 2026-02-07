namespace MonECC.CLI;

/// <summary>
/// Contient toutes les options possibles passées en ligne de commande.
/// </summary>
public class ParsedOptions
{
    public string Command { get; set; } = string.Empty;
    public List<string> PositionalArgs { get; } = new();

    // Options (Switchs)
    public string? OutputFile { get; set; } // -o
    public string? InputFile { get; set; }  // -i
    public string KeyName { get; set; } = "monECC"; // -f (Défaut: monECC)
    public int KeySize { get; set; } = 1000; // -s (Défaut: 1000)
    public bool ShowHelp { get; set; } = false;
}

public static class ArgParser
{
    public static ParsedOptions Parse(string[] args)
    {
        var options = new ParsedOptions();

        if (args.Length == 0 || args.Contains("help") || args.Contains("-h"))
        {
            options.ShowHelp = true;
            return options;
        }

        // Le premier argument est toujours la commande (sauf si c'est un switch, ce qui serait une erreur)
        if (!args[0].StartsWith("-"))
        {
            options.Command = args[0].ToLower();
        }

        for (int i = 1; i < args.Length; i++)
        {
            string current = args[i];

            if (current.StartsWith("-"))
            {
                // C'est un switch, on regarde s'il a une valeur après
                string? value = (i + 1 < args.Length && !args[i + 1].StartsWith("-")) ? args[i + 1] : null;

                switch (current)
                {
                    case "-f":
                        if (value != null) { options.KeyName = value; i++; }
                        break;
                    case "-s":
                        if (value != null && int.TryParse(value, out int size)) { options.KeySize = size; i++; }
                        break;
                    case "-i":
                        if (value != null) { options.InputFile = value; i++; }
                        break;
                    case "-o":
                        if (value != null) { options.OutputFile = value; i++; }
                        break;
                }
            }
            else
            {
                // C'est un argument positionnel (ex: le fichier clé publique, le message...)
                options.PositionalArgs.Add(current);
            }
        }

        return options;
    }
}