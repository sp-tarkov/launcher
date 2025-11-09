namespace SPTarkov.Core.SPT;

public record SPTTypesResponse : IResponse<Dictionary<string, string>>
{
    public Dictionary<string, string>? Response { get; set; } = new();
}
