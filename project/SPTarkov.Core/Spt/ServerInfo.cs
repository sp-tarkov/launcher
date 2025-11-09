namespace SPTarkov.Core.SPT;

public record ServerInfo
{
    public Dictionary<string, string> Types { get; set; } = new();
}
