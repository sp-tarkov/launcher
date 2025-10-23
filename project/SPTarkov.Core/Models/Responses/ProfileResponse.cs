using SPTarkov.Core.Models.Spt;

namespace SPTarkov.Core.Models.Responses;

public record ProfileResponse : ISptResponse<MiniProfile>
{
    public MiniProfile? Response { get; set; }
}
