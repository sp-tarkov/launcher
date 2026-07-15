namespace SPTarkov.Core.Update;

public enum UpdatePhase
{
    Staged,
    Committing,
    Relaunch,
    Cleanup,
}

// Progress marker written by UpdateTransaction and read by UpdateRecovery on the next launch.
public record UpdateState
{
    public required string Version { get; init; }
    public required UpdatePhase Phase { get; init; }
}
