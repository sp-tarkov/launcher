using System.Text.Json.Serialization;
using SPTarkov.Core.Extensions;

namespace SPTarkov.Core.Models;

public record ForgeBase
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("hub_id")]
    public int? HubId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("teaser")]
    public string? Teaser { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("downloads")]
    public int? Downloads { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("source_code_url")]
    public string? SourceCode { get; set; }

    [JsonPropertyName("featured")]
    public bool? Featured { get; set; }

    [JsonPropertyName("contains_ads")]
    public bool? ContainsAds { get; set; }

    [JsonPropertyName("contains_ai_content")]
    public bool? ContainsAiContent { get; set; }

    [JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("owner")]
    public ForgeUser? Owner { get; set; }

    [JsonPropertyName("authors")]
    public List<ForgeUser>? Authors { get; set; }

    [JsonPropertyName("versions")]
    public List<ForgeModVersion>? Versions { get; set; }

    [JsonPropertyName("license")]
    public ForgeLicense? License { get; set; }

    public char? GetAvatarLetter()
    {
        return Owner.Name.ToUpper().FirstOrDefault();
    }

    public bool TryGetProfileAvatar(out string? url)
    {
        url = Owner.ProfilePhotoUrl;
        return !string.IsNullOrEmpty(url);
    }

    public string? GetModName()
    {
        return Name.UppercaseFirst();
    }

    public string? GetModdersName()
    {
        return Owner.Name.UppercaseFirst();
    }

    public string? GetModTeaser()
    {
        return Teaser.UppercaseFirst();
    }

    public bool GetModFeatured()
    {
        return Featured ?? false;
    }

    public string? GetAdditionalAuthors()
    {
        return Authors.Any()
            ? Authors.Select(x => x.Name).Aggregate((i, j) => i + ", " + j)
            : "None";
    }
}
