namespace SPTarkov.Core.SPT.Responses;

public record SPTRegisterResponse : IResponse<bool>
{
    public List<MiniProfile> Profiles { get; set; } = [];

    public bool Response { get; set; }
}
