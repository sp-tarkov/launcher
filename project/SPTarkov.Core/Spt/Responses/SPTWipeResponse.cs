namespace SPTarkov.Core.SPT;

public record SPTWipeResponse : IResponse<bool>
{
    public bool Response { get; set; }

    public List<MiniProfile> Profiles { get; set; } = [];
}
