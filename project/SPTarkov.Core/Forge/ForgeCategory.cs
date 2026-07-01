using System.Text.Json.Serialization;

namespace SPTarkov.Core.Forge;

public class ForgeCategory
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("hub_id")]
    public int? HubId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
