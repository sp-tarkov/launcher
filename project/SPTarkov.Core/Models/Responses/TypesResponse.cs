namespace SPTarkov.Core.Models;

public class TypesResponse : ISptResponse<Dictionary<string, string>>
{
    public Dictionary<string, string> Response { get; set; } = new();
}
