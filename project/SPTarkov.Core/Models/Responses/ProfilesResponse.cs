namespace SPTarkov.Core.Models;

public record ProfilesResponse : ISptResponse<List<MiniProfile>>
{
    public List<MiniProfile> Response { get; set; } = [];
}
