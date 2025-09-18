namespace SPTarkov.Core.Models;

public class ModsResponse : ISptResponse<Dictionary<string, SPTMod>>
{
    public Dictionary<string, SPTMod> Response { get; set; } = new();
}
