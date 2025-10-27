namespace SPTarkov.Core.Spt;

public record ModsResponse : IResponse<Dictionary<string, SptMod>>
{
    public Dictionary<string, SptMod>? Response { get; set; } = new();
}
