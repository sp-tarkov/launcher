using System.Text.Json.Serialization;
using SPTarkov.Core.Semver;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Mods;

public class UpdateTask : IModTask
{
    public required string Name { get; set; }

    [JsonConverter(typeof(SemVerVersionConverter))]
    public required Version Version { get; set; }
    public required string GUID { get; set; }
    public required string Link { get; set; }
    public float Progress { get; set; }
    public long TotalToDownload { get; set; }
    public required CancellationTokenSource CancellationTokenSource { get; set; }
    public bool Complete { get; set; }
    public Exception? Error { get; set; }
}
