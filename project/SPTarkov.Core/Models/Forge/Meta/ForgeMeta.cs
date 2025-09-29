using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public record ForgeMeta
{
    [JsonPropertyName("current_page")]
    public int? CurrentPage { get; set; }

    [JsonPropertyName("from")]
    public int? From { get; set; }

    [JsonPropertyName("last_page")]
    public int? LastPage { get; set; }

    [JsonPropertyName("links")]
    public List<ForgeMetaLinks>? Links { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("per_page")]
    public int? PerPage { get; set; }

    [JsonPropertyName("to")]
    public int? To { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }
}
