using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public class ForgeUser
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("profile_photo_url")]
    public string? ProfilePhotoUrl { get; set; }

    [JsonPropertyName("cover_photo_url")]
    public string? CoverPhotoUrl { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}
