namespace SPTarkov.Core.Spt;

public record ProfilesResponse : IResponse<List<MiniProfile>>
{
    public List<MiniProfile>? Response { get; set; } = [];
}
