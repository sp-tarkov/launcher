using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.Update;

/// <summary>The self-update commit/rollback protocol.</summary>
public class UpdateTransaction(ILogger<UpdateTransaction> logger)
{
    private const int DeleteRetries = 12;
    private const int DeleteDelayMs = 250;

    /// <summary>Wipes any previous transaction's staging and marks a new update as staged.</summary>
    public void Begin(string version)
    {
        if (Directory.Exists(UpdateLayout.Staging))
        {
            Directory.Delete(UpdateLayout.Staging, true);
        }

        Directory.CreateDirectory(UpdateLayout.Staging);
        WriteState(new UpdateState { Version = version, Phase = UpdatePhase.Staged });
    }

    /// <summary>Applies the extracted payload to the install, rolling back and rethrowing if any step fails.</summary>
    public void Commit(string version)
    {
        File.Delete(UpdateLayout.OldExePath);

        WriteState(new UpdateState { Version = version, Phase = UpdatePhase.Committing });

        try
        {
            ApplyDataFiles();
            ApplyDormantExe();
            SwapRunningExe();
        }
        catch (Exception)
        {
            RollBack();
            throw;
        }

        try
        {
            WriteState(new UpdateState { Version = version, Phase = UpdatePhase.Relaunch });
        }
        catch (Exception ex)
        {
            logger.LogWarning("Could not advance the update marker to Relaunch: {Exception}", ex);
        }
    }

    public UpdateState? ReadState()
    {
        if (!File.Exists(UpdateLayout.StateFile))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<UpdateState>(File.ReadAllText(UpdateLayout.StateFile));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public bool HasLeftovers()
    {
        return Directory.Exists(UpdateLayout.Staging) || File.Exists(UpdateLayout.OldExePath);
    }

    public bool SwapCompleted()
    {
        return File.Exists(UpdateLayout.OldExePath) && File.Exists(UpdateLayout.RunningExePath);
    }

    /// <summary>Keeps the new build. Drops the pre-update backups.</summary>
    public void RollForward()
    {
        RestoreExeIfMissing();
        DeleteBackups();
    }

    /// <summary>Returns to the pre-update build. Puts the old exe back and restores every backed-up file.</summary>
    public void RollBack()
    {
        RestoreExeIfMissing();
        RestoreBackups();
    }

    /// <summary>Restores the exe if only the old one survived, then drops the old exe and the staging directory.</summary>
    public void Cleanup(UpdateState state)
    {
        RestoreExeIfMissing();

        var oldExeDeleted = DeleteWithRetry(UpdateLayout.OldExePath);
        var stagingDeleted = DeleteStaging();

        if (oldExeDeleted && stagingDeleted)
        {
            ClearState();
            logger.LogInformation("Update leftovers removed.");
        }
        else
        {
            logger.LogWarning("Some update leftovers could not be removed; the cleanup will run again on the next launch.");
            WriteState(state with { Phase = UpdatePhase.Cleanup });
        }
    }

    private static void ApplyDataFiles()
    {
        var extractedData = Path.Combine(UpdateLayout.StagingExtract, "SPT_Data", "Launcher");
        if (!Directory.Exists(extractedData))
        {
            return;
        }

        foreach (var source in Directory.EnumerateFiles(extractedData, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(UpdateLayout.StagingExtract, source);
            var destination = Path.Combine(UpdateLayout.InstallRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            if (File.Exists(destination))
            {
                File.Copy(destination, destination + UpdateLayout.BackupSuffix, true);
            }

            File.Copy(source, destination, true);
        }
    }

    private static void ApplyDormantExe()
    {
        var source = Path.Combine(UpdateLayout.StagingExtract, UpdateLayout.DormantExeName);
        if (!File.Exists(source))
        {
            return;
        }

        var destination = Path.Combine(UpdateLayout.InstallRoot, UpdateLayout.DormantExeName);
        if (File.Exists(destination))
        {
            File.Copy(destination, destination + UpdateLayout.BackupSuffix, true);
        }

        File.Copy(source, destination, true);
    }

    private static void SwapRunningExe()
    {
        var staged = Path.Combine(UpdateLayout.StagingExtract, UpdateLayout.RunningExeName);

        if (!OperatingSystem.IsWindows())
        {
            MakeExecutable(staged);
        }

        File.Move(UpdateLayout.RunningExePath, UpdateLayout.OldExePath);
        File.Move(staged, UpdateLayout.RunningExePath);
    }

    [UnsupportedOSPlatform("windows")]
    private static void MakeExecutable(string path)
    {
        var mode = File.GetUnixFileMode(path);
        File.SetUnixFileMode(path, mode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
    }

    private void RestoreExeIfMissing()
    {
        if (!File.Exists(UpdateLayout.RunningExePath) && File.Exists(UpdateLayout.OldExePath))
        {
            logger.LogWarning("The launcher executable is missing; restoring the previous build.");
            File.Move(UpdateLayout.OldExePath, UpdateLayout.RunningExePath);
        }
    }

    private void RestoreBackups()
    {
        var backups = GetBackups();

        foreach (var backup in backups)
        {
            var original = backup[..^UpdateLayout.BackupSuffix.Length];
            File.Move(backup, original, true);
        }

        if (backups.Count > 0)
        {
            logger.LogInformation("Restored {Count} backed-up files.", backups.Count);
        }
    }

    private void DeleteBackups()
    {
        foreach (var backup in GetBackups())
        {
            try
            {
                File.Delete(backup);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Could not remove update backup {Path}: {Exception}", backup, ex);
            }
        }
    }

    // Enumerates every backup Commit() can write: the data files and the dormant exe.
    private static List<string> GetBackups()
    {
        var backups = new List<string>();

        if (Directory.Exists(UpdateLayout.DataRoot))
        {
            backups.AddRange(Directory.EnumerateFiles(UpdateLayout.DataRoot, "*" + UpdateLayout.BackupSuffix, SearchOption.AllDirectories));
        }

        var dormantBackup = Path.Combine(UpdateLayout.InstallRoot, UpdateLayout.DormantExeName) + UpdateLayout.BackupSuffix;
        if (File.Exists(dormantBackup))
        {
            backups.Add(dormantBackup);
        }

        return backups;
    }

    private bool DeleteStaging()
    {
        try
        {
            if (Directory.Exists(UpdateLayout.Staging))
            {
                Directory.Delete(UpdateLayout.Staging, true);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Could not delete the update staging directory: {Exception}", ex);
            return false;
        }
    }

    private bool DeleteWithRetry(string path)
    {
        Exception? lastError = null;

        for (var attempt = 0; attempt < DeleteRetries; attempt++)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return true;
                }

                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex;
                Thread.Sleep(DeleteDelayMs);
            }
        }

        logger.LogWarning("Could not delete {Path}: {Exception}", path, lastError);
        return false;
    }

    private static void WriteState(UpdateState state)
    {
        Directory.CreateDirectory(UpdateLayout.Staging);

        var temp = UpdateLayout.StateFile + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(state));
        File.Move(temp, UpdateLayout.StateFile, true);
    }

    private static void ClearState()
    {
        if (File.Exists(UpdateLayout.StateFile))
        {
            File.Delete(UpdateLayout.StateFile);
        }
    }
}
