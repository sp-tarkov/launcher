namespace SPTarkov.Core.SPT;

public record SPTProfileResponse : IResponse<MiniProfile>
{
    public MiniProfile? Response { get; set; }
}
