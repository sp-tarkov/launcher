/* FilePatcher.cs
 * License: NCSA Open Source License
 * 
 * Copyright: Merijn Hendriks
 * AUTHORS:
 * Merijn Hendriks
 * waffle.lord
 */

using System;
using System.IO;
using Aki.ByteBanger;
using Aki.Launcher.Controllers;
using Aki.Launcher.MiniCommon;
using Aki.Launcher.Models.Launcher;

namespace Aki.Launcher.Helpers
{
    public static class FilePatcher
    {
        public static event EventHandler<ProgressInfo> PatchProgress;
        private static void RaisePatchProgress(int Percentage, string Message)
        {
            PatchProgress?.Invoke(null, new ProgressInfo(Percentage, Message));
        }

        public static PatchResultInfo Patch(string targetfile, string patchfile, bool IgnoreInputHashMismatch = false)
        {
            byte[] target = VFS.ReadFile(targetfile);
            byte[] patch = VFS.ReadFile(patchfile);

            PatchResult result = PatchUtil.Patch(target, PatchInfo.FromBytes(patch));

            switch (result.Result)
            {
                case PatchResultType.Success:
                    File.Copy(targetfile, $"{targetfile}.bak");
                    VFS.WriteFile(targetfile, result.PatchedData);
                    break;

                case PatchResultType.InputChecksumMismatch:
                    if (IgnoreInputHashMismatch)
                        return new PatchResultInfo(PatchResultType.Success, 1, 1);
                    break;
            }
            
            return new PatchResultInfo(result.Result, 1, 1);
        }

        private static PatchResultInfo PatchAll(string targetpath, string patchpath, bool IgnoreInputHashMismatch = false)
        {
            DirectoryInfo di = new DirectoryInfo(patchpath);

            // get all patch files within patchpath and it's sub directories.
            var patchfiles = di.GetFiles("*.bpf", SearchOption.AllDirectories);

            int countfiles = patchfiles.Length;

            int processed = 0;

            foreach (FileInfo file in patchfiles)
            {
                FileInfo target;

                int progress = (int)Math.Floor((double)processed / countfiles * 100);
                RaisePatchProgress(progress, $"{LocalizationProvider.Instance.patching} {file.Name} ...");

                // get the relative portion of the patch file that will be appended to targetpath in order to create an official target file.
                var relativefile = file.FullName.Substring(patchpath.Length).TrimStart('\\', '/');

                // create a target file from the relative patch file while utilizing targetpath as the root directory.
                target = new FileInfo(VFS.Combine(targetpath, relativefile.Replace(".bpf", "")));

                PatchResultInfo result = Patch(target.FullName, file.FullName, IgnoreInputHashMismatch);

                if (!result.OK)
                {
                    // patch failed
                    return result;
                }

                processed++;
            }

            RaisePatchProgress(100, LocalizationProvider.Instance.ok);

            return new PatchResultInfo(PatchResultType.Success, processed, countfiles);
        }

        public static PatchResultInfo Run(string targetPath, string patchPath, bool IgnoreInputHashMismatch = false)
        {
            return PatchAll(targetPath, patchPath, IgnoreInputHashMismatch);
        }

        public static void Restore(string filepath)
        {
            RestoreRecurse(new DirectoryInfo(filepath));
        }

        static void RestoreRecurse(DirectoryInfo basedir)
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
                if (file.Extension == ".bak")
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
                        file.IsReadOnly = false;
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        LogManager.Instance.Exception(ex);
                    }
                }
            }
        }
    }
}