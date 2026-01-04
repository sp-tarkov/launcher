using System.ComponentModel;
using System.Text.Json.Serialization;
using SPTarkov.Core.Semver;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Mods;

public class UpdateTask : IModTask
{
    public string ModName { get; set; }

    [JsonConverter(typeof(SemVerVersionConverter))]
    public Version Version { get; set; }
    public string GUID { get; set; }
    public string Link { get; set; }
    public float Progress { get; set; }
    public float TotalToDownload { get; set; }
    public required CancellationTokenSource CancellationTokenSource { get; set; }
    public bool Complete { get; set; }
    public Exception Error { get; set; }
}
