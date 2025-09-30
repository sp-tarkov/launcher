namespace SPTarkov.Core.Models;

public record ProfileResponse : ISptResponse<MiniProfile>
{
    public MiniProfile? Response { get; set; }
}
