namespace SPTarkov.Core.SPT.Responses;

public record SPTWipeResponse : IResponse<bool>
{
    public bool Response { get; set; }

    public List<MiniProfile> Profiles { get; set; } = [];
}
