using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge.Responses;

public class ForgeUpdateResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<ForgeModUpdates>? Data { get; set; }
}
