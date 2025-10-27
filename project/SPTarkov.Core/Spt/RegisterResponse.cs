namespace SPTarkov.Core.Spt;

public record RegisterResponse : IResponse<bool>
{
    public List<MiniProfile> Profiles { get; set; } = [];

    public bool Response { get; set; }
}
