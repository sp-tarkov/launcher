using SPTarkov.Core.Models.Spt;

namespace SPTarkov.Core.Models.Responses;

public record ModsResponse : ISptResponse<Dictionary<string, SptMod>>
{
    public Dictionary<string, SptMod>? Response { get; set; } = new();
}
