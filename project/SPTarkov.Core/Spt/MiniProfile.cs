using System.Text.Json.Serialization;

namespace SPTarkov.Core.SPT;

public record MiniProfile
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = "";

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

    [JsonPropertyName("profileCurrency")]
    public ProfileCurrency ProfileCurrency { get; set; } = new();

    [JsonPropertyName("profileStats")]
    public ProfileStats ProfileStats { get; set; } = new();

    [JsonPropertyName("wipe")]
    public bool Wipe { get; set; } = false;
}

public record ProfileStats
{
    [JsonPropertyName("overall")]
    public Dictionary<string, string> Overall { get; set; } = new();

    [JsonPropertyName("pmc")]
    public Dictionary<string, string> Pmc { get; set; } = new();

    [JsonPropertyName("scav")]
    public Dictionary<string, string> Scav { get; set; } = new();
}

public record ProfileCurrency
{
    [JsonPropertyName("roubles")]
    public int Roubles { get; set; } = 0;

    [JsonPropertyName("euros")]
    public int Euros { get; set; } = 0;

    [JsonPropertyName("dollars")]
    public int Dollars { get; set; } = 0;

    [JsonPropertyName("gp")]
    public int GP { get; set; } = 0;
}
