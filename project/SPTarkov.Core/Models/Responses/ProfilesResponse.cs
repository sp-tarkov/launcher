using SPTarkov.Core.Models.Spt;

namespace SPTarkov.Core.Models.Responses;

public record ProfilesResponse : ISptResponse<List<MiniProfile>>
{
    public List<MiniProfile>? Response { get; set; } = [];
}
