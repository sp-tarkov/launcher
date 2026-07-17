namespace SPTarkov.Core.Update;

/// <summary>Carries the version applied by <see cref="UpdateRecovery"/> at startup to the UI.</summary>
public class UpdateNotice
{
    public string? JustUpdatedVersion { get; set; }
}
