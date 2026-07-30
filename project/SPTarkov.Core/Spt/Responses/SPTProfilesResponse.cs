namespace SPTarkov.Core.SPT.Responses;

public record SPTProfilesResponse : IResponse<List<MiniProfile>>
{
    public List<MiniProfile>? Response { get; set; } = [];
}
