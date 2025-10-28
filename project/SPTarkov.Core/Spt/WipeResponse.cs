namespace SPTarkov.Core.Spt;

public record WipeResponse : IResponse<bool>
{
    public bool Response { get; set; }

    public List<MiniProfile> Profiles { get; set; } = [];
}
