namespace SPTarkov.Core.Mods.Inspection;

/// <summary>
/// A mod component discovered inside a DLL: a BepInEx client plugin or an SPT server mod metadata type. A null Guid,
/// Name, or Version means the component was detected but that value could not be recovered from the DLL's metadata.
/// </summary>
public sealed record DllFinding(string Path, DllComponentKind Kind, string? Guid, string? Name, string? Version);
