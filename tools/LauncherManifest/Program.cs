using SPTarkov.Tools.LauncherManifest;

try
{
    return ManifestBuilder.Run(args);
}
catch (ManifestException ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 1;
}
