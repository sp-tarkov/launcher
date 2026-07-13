namespace SPTarkov.Core.Update;

// Carries the version applied by startup update recovery to the UI.
public class UpdateNotice
{
    public string? JustUpdatedVersion { get; set; }
}
