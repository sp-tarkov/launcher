namespace SPTarkov.Core.Models;

public record ModsResponse : ISptResponse<Dictionary<string, SPTMod>>
{
    public Dictionary<string, SPTMod> Response { get; set; } = new();
}
