using System.Text.Json.Serialization;
using SPTarkov.Core.Models.Forge;
using SPTarkov.Core.Models.Forge.Links;
using SPTarkov.Core.Models.Forge.Meta;

namespace SPTarkov.Core.Models.Responses;

public record ForgeModsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<ForgeBase>? Data { get; set; }

    [JsonPropertyName("links")]
    public ForgeLinks? Links { get; set; }

    [JsonPropertyName("meta")]
    public ForgeMeta? Meta { get; set; }
}
