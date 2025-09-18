using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public class ForgeLicense
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("hub_id")]
    public int? HubId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("link")]
    public string? Link { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}
