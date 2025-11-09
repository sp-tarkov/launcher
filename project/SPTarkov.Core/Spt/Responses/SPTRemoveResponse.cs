namespace SPTarkov.Core.SPT;

public record SPTRemoveResponse : IResponse<bool>
{
    public List<MiniProfile> Profiles { get; set; } = [];

    public bool Response { get; set; }
}
