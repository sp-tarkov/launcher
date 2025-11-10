namespace SPTarkov.Core.SPT.Responses;

public record SPTModsResponse : IResponse<Dictionary<string, SptMod>>
{
    public Dictionary<string, SptMod>? Response { get; set; } = new();
}
