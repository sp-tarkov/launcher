namespace SPTarkov.Core.SPT;

public record SPTModsResponse : IResponse<Dictionary<string, SptMod>>
{
    public Dictionary<string, SptMod>? Response { get; set; } = new();
}
