namespace SPTarkov.Core.Mods.Inspection;

/// <summary>The kind of mod component a DLL carries: a BepInEx client plugin or an SPT server mod.</summary>
public enum DllComponentKind
{
    Client,
    Server,
}
