using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.Update;

/// <summary>Checks that an update can be applied. <see cref="Check"/> returns <see cref="UpdateFailure.None"/> when it can.</summary>
public class UpdatePreflight(ILogger<UpdatePreflight> logger)
{
    public UpdateFailure Check(long payloadSize)
    {
        if (!HasFreeSpace(payloadSize))
        {
            return UpdateFailure.InsufficientSpace;
        }

        return CanWrite() ? UpdateFailure.None : UpdateFailure.NotWritable;
    }

    private bool HasFreeSpace(long payloadSize)
    {
        try
        {
            var root = Path.GetPathRoot(UpdateLayout.InstallRoot);
            if (string.IsNullOrEmpty(root))
            {
                return true;
            }

            // Leaves room for the download, the extracted copy, and the backups.
            return new DriveInfo(root).AvailableFreeSpace > payloadSize * 4;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Could not determine free space: {Exception}", ex);
            return true;
        }
    }

    private bool CanWrite()
    {
        var probe = Path.Combine(UpdateLayout.InstallRoot, $".probe-{Guid.NewGuid():N}");
        var renamed = probe + ".renamed";
        var staged = Path.Combine(UpdateLayout.Staging, Path.GetFileName(renamed));
        var replaceTarget = Path.Combine(UpdateLayout.DataRoot, $".probe-{Guid.NewGuid():N}");
        var replaceSource = replaceTarget + ".new";

        try
        {
            Directory.CreateDirectory(UpdateLayout.Staging);
            Directory.CreateDirectory(UpdateLayout.DataRoot);

            // Create, write, and rename in the install directory.
            File.WriteAllText(probe, "probe");
            File.Move(probe, renamed);

            // Move into the staging directory and back.
            File.Move(renamed, staged);
            File.Move(staged, renamed);
            File.Delete(renamed);

            // Atomic replace under SPT_Data/Launcher.
            File.WriteAllText(replaceTarget, "target");
            File.WriteAllText(replaceSource, "source");
            File.Replace(replaceSource, replaceTarget, null);
            File.Delete(replaceTarget);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Update preflight write probe failed: {Exception}", ex);
            return false;
        }
        finally
        {
            foreach (var leftover in new[] { probe, renamed, staged, replaceTarget, replaceSource })
            {
                try
                {
                    if (File.Exists(leftover))
                    {
                        File.Delete(leftover);
                    }
                }
                catch
                {
                    // Best-effort cleanup.
                }
            }
        }
    }
}
