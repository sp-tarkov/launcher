namespace SPTarkov.Core.Spt;

public record RemoveResponse : IResponse<bool>
{
    public List<MiniProfile> Profiles { get; set; } = [];

    public bool Response { get; set; }
}
