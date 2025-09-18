using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public class ForgeResponseData
{
    [JsonPropertyName("token")] public string? Token { get; set; }
}
