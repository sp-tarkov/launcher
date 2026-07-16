using System.Text.Json.Serialization;
using SPTarkov.Core.Semver;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Forge;

public record ForgeModVersion
{
    [JsonPropertyName("id")]
    public required int Id { get; set; }

    [JsonPropertyName("hub_id")]
    public int? HubId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("link")]
    public required string Link { get; set; }

    [JsonPropertyName("content_length")]
    public long? ContentLength { get; set; }

    [JsonPropertyName("downloads")]
    public int? Downloads { get; set; }

    [JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("fika_compatibility")]
    public string? FikaCompatible { get; set; }

    [JsonPropertyName("virus_total_links")]
    public List<ForgeListOfLinks>? VirusTotalLinks { get; set; }

    [JsonPropertyName("dependencies")]
    public List<ForgeBase>? Dependencies { get; set; }

    [JsonPropertyName("version")]
    [JsonConverter(typeof(SemVerVersionConverter))]
    public required Version Version { get; set; }

    [JsonPropertyName("spt_version_constraint")]
    [JsonConverter(typeof(SemVerRangeConverter))]
    public Range? SptVersionConstraint { get; set; }

    [JsonPropertyName("mod_version_constraint")]
    [JsonConverter(typeof(SemVerRangeConverter))]
    public Range? ModVersionConstraint { get; set; }

    private string GetDateString(string date)
    {
        var dated = DateTime.Parse(date);
        return dated.ToString("dd-MM-yy");
    }

    public string GetCreatedDateFormatted()
    {
        return GetDateString(CreatedAt!);
    }

    public string GetUpdatedDateFormatted()
    {
        return GetDateString(UpdatedAt!);
    }

    public string GetPublishedDateFormatted()
    {
        return GetDateString(PublishedAt!);
    }

    public string? GetContentLengthFormatted()
    {
        if (ContentLength is not > 0)
        {
            return null;
        }

        string[] units = ["B", "KB", "MB", "GB"];
        double size = ContentLength.Value;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#} {units[unit]}";
    }
}
