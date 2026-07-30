namespace SPTarkov.Core.Configuration;

public record PreferredProfile
{
    public string ServerId { get; set; } = "";

    public string ProfileId { get; set; } = "";
}
