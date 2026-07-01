using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge.Responses;

public class ForgeCategoriesResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<ForgeCategory>? Data { get; set; }

    [JsonPropertyName("links")]
    public ForgeLinks? Links { get; init; }

    [JsonPropertyName("meta")]
    public ForgeMeta? Meta { get; init; }
}
