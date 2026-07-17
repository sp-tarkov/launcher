namespace SPTarkov.Core.Update;

public enum UpdatePhase
{
    Staged,
    Committing,
    Relaunch,
    Cleanup,
}

/// <summary>Progress marker written by <see cref="UpdateTransaction"/> and read by <see cref="UpdateRecovery"/> on the next launch.</summary>
public record UpdateState
{
    public required string Version { get; init; }
    public required UpdatePhase Phase { get; init; }
}
