using System.Text.Json.Serialization;

namespace SPTarkov.Core.SPT;

public record MiniProfile
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("side")]
    public string Side { get; set; } = "";

    [JsonPropertyName("currlvl")]
    public int CurrentLevel { get; set; }

    [JsonPropertyName("currexp")]
    public long CurrentExp { get; set; }

    [JsonPropertyName("prevexp")]
    public long PreviousExp { get; set; }

    [JsonPropertyName("nextlvl")]
    public long NextLevel { get; set; }

    [JsonPropertyName("maxlvl")]
    public int MaxLevel { get; set; }

    [JsonPropertyName("profileId")]
    public string ProfileId { get; set; } = "";

    [JsonPropertyName("edition")]
    public string Edition { get; set; } = "";

    [JsonPropertyName("sptData")]
    public SptData SptData { get; set; } = new();

    [JsonPropertyName("invalidOrUnloadableProfile")]
    public bool InvalidOrUnloadableProfile { get; set; }
}
