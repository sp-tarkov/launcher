using System.Text.Json.Serialization;
using SPTarkov.Core.Models.Forge;
using SPTarkov.Core.Models.Forge.Links;
using SPTarkov.Core.Models.Forge.Meta;

namespace SPTarkov.Core.Models.Responses;

public record ForgeVersionResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<ForgeModVersion>? Data { get; set; }

    [JsonPropertyName("links")]
    public ForgeLinks? Links { get; set; }

    [JsonPropertyName("meta")]
    public ForgeMeta? Meta { get; set; }
}
