using System.Text.Json.Serialization;
using SPTarkov.Core.Models.Forge;

namespace SPTarkov.Core.Models.Responses;

public record ForgeModResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public ForgeBase? Data { get; set; }
}
