namespace SPTarkov.Core.Spt;

public record ServerInfo
{
    public Dictionary<string, string> Types { get; set; } = new();
}
