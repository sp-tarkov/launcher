namespace SPTarkov.Core.Spt;

public record RequestUrl
{
    public const string Ping = "/launcher/v2/ping";
    public const string Types = "/launcher/v2/types";
    public const string Login = "/launcher/v2/login";
    public const string Register = "/launcher/v2/register";
    public const string Remove = "/launcher/v2/remove";
    public const string Version = "/launcher/v2/version";
    public const string Mods = "/launcher/v2/mods";
    public const string Profiles = "/launcher/v2/profiles";
    public const string Profile = "/launcher/v2/profile";

    public const string ForgeMods = "/api/v0/mods";
}
