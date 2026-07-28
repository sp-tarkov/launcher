using SPTarkov.Core.Forge;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Mods;

/// <summary>A mod version's dependency tree, flattened and partitioned by whether each dependency can be downloaded.</summary>
public class DependencyResolution
{
    /// <summary>The immediate dependencies of the resolved mod version.</summary>
    public List<ForgeDependencyNode> DirectDependencies { get; init; } = [];

    /// <summary>Dependencies with a single compatible version, deduplicated across the tree.</summary>
    public List<ForgeDependencyNode> Resolved { get; } = [];

    /// <summary>Dependencies whose version constraints are incompatible; one entry per conflicting version.</summary>
    public List<ForgeDependencyNode> Conflicted { get; } = [];

    /// <summary>Dependencies with no compatible version or no usable identity.</summary>
    public List<ForgeDependencyNode> Unresolvable { get; } = [];

    /// <summary>Installed mods whose dependency constraints the target version does not satisfy.</summary>
    public List<DependentViolation> DependentViolations { get; } = [];

    /// <summary>Whether the tree contains problems that must block the operation.</summary>
    public bool IsBlocked => Conflicted.Count > 0 || Unresolvable.Count > 0 || DependentViolations.Count > 0;
}

/// <summary>A dependent constraint the target version does not satisfy.</summary>
public record DependentViolation(string GUID, string Name, Version TargetVersion, Version? RequiredVersion, List<string> Dependents);
