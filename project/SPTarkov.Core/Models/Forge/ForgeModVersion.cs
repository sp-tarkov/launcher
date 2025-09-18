using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public class ForgeModVersion
{
    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("hub_id")] public int? HubId { get; set; }

    [JsonPropertyName("version")] public string? Version { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("link")] public string? Link { get; set; }

    [JsonPropertyName("spt_version_constraint")]
    public string? SptVersionConstraint { get; set; }

    [JsonPropertyName("virus_total_link")] public string? VirusTotalLink { get; set; }

    [JsonPropertyName("downloads")] public int? Downloads { get; set; }

    [JsonPropertyName("published_at")] public string? PublishedAt { get; set; }

    [JsonPropertyName("created_at")] public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")] public string? UpdatedAt { get; set; }

    private string? GetDateString(string date)
    {
        var dated = DateTime.Parse(date);
        return dated.ToString("dd-MM-yy");
    }

    public string? GetCreatedDateFormatted()
    {
        return GetDateString(CreatedAt);
    }

    public string? GetUpdatedDateFormatted()
    {
        return GetDateString(UpdatedAt);
    }

    public string? GetPublishedDateFormatted()
    {
        return GetDateString(PublishedAt);
    }
}
