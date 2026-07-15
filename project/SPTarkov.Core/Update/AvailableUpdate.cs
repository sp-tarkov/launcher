namespace SPTarkov.Core.Update;

public record AvailableUpdate
{
    public required ReleaseEntry Release { get; init; }

    public required Version CurrentVersion { get; init; }
}
