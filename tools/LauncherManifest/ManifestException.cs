namespace SPTarkov.Tools.LauncherManifest;

// Thrown when a release fails a manifest rule.
public sealed class ManifestException(string message) : Exception(message);
