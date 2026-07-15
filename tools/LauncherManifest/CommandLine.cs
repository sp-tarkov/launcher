namespace SPTarkov.Tools.LauncherManifest;

// Parses "--key value" pairs into typed lookups.
internal sealed class CommandLine
{
    private readonly Dictionary<string, string> _values;

    private CommandLine(Dictionary<string, string> values)
    {
        _values = values;
    }

    public static CommandLine Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = args[i][2..];
            var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
            values[key] = hasValue ? args[++i] : "";
        }

        return new CommandLine(values);
    }

    public string Required(string key)
    {
        return _values.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value)
            ? value
            : throw new ManifestException($"missing required argument --{key}");
    }

    public string? Optional(string key)
    {
        return _values.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : null;
    }
}
