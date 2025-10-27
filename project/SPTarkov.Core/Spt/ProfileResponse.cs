namespace SPTarkov.Core.Spt;

public record ProfileResponse : IResponse<MiniProfile>
{
    public MiniProfile? Response { get; set; }
}
