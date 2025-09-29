namespace SPTarkov.Core.Models;

public record Server
{
    public string Name { get; set; } = "LocalHost";

    public string IpAddress { get; set; } = "127.0.0.1:6969";

    public string ServerId { get; set; } = "1721162719";

    public bool Locked { get; set; } = true;
}
