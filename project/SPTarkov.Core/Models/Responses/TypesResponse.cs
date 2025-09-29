namespace SPTarkov.Core.Models;

public record TypesResponse : ISptResponse<Dictionary<string, string>>
{
    public Dictionary<string, string> Response { get; set; } = new();
}
