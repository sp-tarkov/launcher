namespace SPTarkov.Core.SPT.Responses;

public record SPTProfileResponse : IResponse<MiniProfile>
{
    public MiniProfile? Response { get; set; }
}
