namespace SPTarkov.Core.Mods;

public enum ModOperation
{
    Downloaded,
    Installed,
    Uninstalled,
    Updated,
    Deleted,
}

/// <summary>A completed mod operation, raised for UI notification.</summary>
public record ModNotification(ModOperation Operation, string GUID, string Name);
