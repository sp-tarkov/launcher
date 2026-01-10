/* FilePatcher.cs
 * License: NCSA Open Source License
 *
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */

using Microsoft.Extensions.Logging;
using SharpHDiffPatch.Core;

namespace SPTarkov.Core.Patching;

public class FilePatcher(ILogger<FilePatcher> logger)
{
    private PatchResultInfo Patch(string targetfile, string patchfile, bool ignoreInputHashMismatch = false)
    {
        // Backup the original file if a backup doesn't exist yet
        var backupFile = $"{targetfile}.spt-bak";
        if (!File.Exists(backupFile))
        {
            File.Copy(targetfile, backupFile);
        }

        var result = ApplyPatch(patchfile, backupFile, targetfile);
        if (result.Status == PatchResultEnum.InputChecksumMismatch && ignoreInputHashMismatch)
        {
            return new PatchResultInfo(PatchResultEnum.Success, 1, 1);
        }

        return result;
    }

    private PatchResultInfo PatchAll(string targetpath, string patchpath, bool ignoreInputHashMismatch = false)
    {
        var di = new DirectoryInfo(patchpath);

        // get all patch files within patchpath and it's sub directories.
        var patchfiles = di.GetFiles("*.delta", SearchOption.AllDirectories);

        var countfiles = patchfiles.Length;

        var processed = 0;

        foreach (FileInfo file in patchfiles)
        {
            FileInfo target;

            // get the relative portion of the patch file that will be appended to targetpath in order to create an official target file.
            var relativefile = file.FullName.Substring(patchpath.Length).TrimStart('\\', '/');

            // create a target file from the relative patch file while utilizing targetpath as the root directory.
            target = new FileInfo(Path.Join(targetpath, relativefile.Replace(".delta", "")));

            var result = Patch(target.FullName, file.FullName, ignoreInputHashMismatch);

            if (!result.Ok)
            {
                // patch failed
                return result;
            }

            processed++;
        }

        return new PatchResultInfo(PatchResultEnum.Success, processed, countfiles);
    }

    public PatchResultInfo Run(string targetPath, string patchPath, bool ignoreInputHashMismatch = false)
    {
        return PatchAll(targetPath, patchPath, ignoreInputHashMismatch);
    }

    public void Restore(string filepath)
    {
        RestoreRecurse(new DirectoryInfo(filepath));
    }

    private void RestoreRecurse(DirectoryInfo basedir)
    {
        // scan subdirectories
        foreach (var dir in basedir.EnumerateDirectories())
        {
            RestoreRecurse(dir);
        }

        // scan files
        var files = basedir.GetFiles();

        foreach (var file in files)
        {
            if (file.Extension == ".spt-bak")
            {
                var target = Path.ChangeExtension(file.FullName, null);

                // remove patched file
                try
                {
                    var patched = new FileInfo(target);
                    patched.IsReadOnly = false;
                    patched.Delete();

                    // Restore from backup
                    File.Copy(file.FullName, target);
                }
                catch (Exception ex)
                {
                    logger.LogError("exception thrown: {ex}", ex);
                }
            }
        }
    }

    private PatchResultInfo ApplyPatch(string patchFilePath, string sourceFilePath, string targetFilePath)
    {
        // TODO: We should do checksum validation at some point
        try
        {
            var patcher = new HDiffPatch();
            HDiffPatch.LogVerbosity = Verbosity.Quiet;

            patcher.Initialize(patchFilePath);
            patcher.Patch(sourceFilePath, targetFilePath, false);
        }
        catch (Exception ex)
        {
            logger.LogError("exception thrown: {ex}", ex);
            return new PatchResultInfo(PatchResultEnum.InputLengthMismatch, 1, 1);
        }

        return new PatchResultInfo(PatchResultEnum.Success, 1, 1);
    }
}
