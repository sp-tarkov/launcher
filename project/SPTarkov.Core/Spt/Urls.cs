namespace SPTarkov.Core.SPT;

public class Urls
{
    public const string Ping = "/launcher/v2/ping";
    public const string Types = "/launcher/v2/types";
    public const string Login = "/launcher/v2/login";
    public const string Register = "/launcher/v2/register";
    public const string Remove = "/launcher/v2/remove";
    public const string Version = "/launcher/v2/version";
    public const string ModPages = "/launcher/v2/mod-pages";
    public const string Profiles = "/launcher/v2/profiles";
    public const string Profile = "/launcher/v2/profile";
    public const string Wipe = "/launcher/v2/wipe";

    public const string UpdateManifestStable = "https://launcher-auto-update.sp-tarkov.com/stable.json";
    public const string UpdateManifestStableSignature = "https://launcher-auto-update.sp-tarkov.com/stable.json.sig";
    public const string UpdateManifestEdge = "https://launcher-auto-update.sp-tarkov.com/edge.json";
    public const string UpdateManifestEdgeSignature = "https://launcher-auto-update.sp-tarkov.com/edge.json.sig";
}
