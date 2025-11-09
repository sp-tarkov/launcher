namespace SPTarkov.Core.SPT;

public record SPTProfilesResponse : IResponse<List<MiniProfile>>
{
    public List<MiniProfile>? Response { get; set; } = [];
}
