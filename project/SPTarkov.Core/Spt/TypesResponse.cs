namespace SPTarkov.Core.Spt;

public record TypesResponse : IResponse<Dictionary<string, string>>
{
    public Dictionary<string, string>? Response { get; set; } = new();
}
