namespace SPTarkov.Core.Models;

public record ServerInfo
{
    public Dictionary<string, string> Types { get; set; } = new();
}
