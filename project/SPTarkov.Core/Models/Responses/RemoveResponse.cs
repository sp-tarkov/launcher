using SPTarkov.Core.Models.Spt;

namespace SPTarkov.Core.Models.Responses;

public record RemoveResponse : ISptResponse<bool>
{
    public List<MiniProfile> Profiles { get; set; } = [];

    public bool Response { get; set; }
}
