/* ProgressReportingPatchRunner.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */

using SPT.Launcher.MiniCommon;
using SPT.Launcher.Models.Launcher;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SPT.Launcher.Helpers
{
    public class ProgressReportingPatchRunner
    {
        private string GamePath;
        private string[] Patches;

        private async IAsyncEnumerable<PatchResultInfo> TryPatchFiles(bool IgnoreInputHashMismatch)
        {
            FilePatcher.Restore(GamePath);

            int processed = 0;
            int countpatches = Patches.Length;

            var _patches = Patches;
            foreach (var patch in _patches)
            {
                var result =
                    await Task.Factory.StartNew(() => FilePatcher.Run(GamePath, patch, IgnoreInputHashMismatch));
                if (!result.OK)
                {
                    yield return new PatchResultInfo(result.Status, processed, countpatches);
                    yield break;
                }
                
                processed++;
                var ourResult = new PatchResultInfo(PatchResultType.Success, processed, countpatches);
                yield return ourResult;
            }
        }

        public async IAsyncEnumerable<PatchResultInfo> PatchFiles()
        {
            await foreach (var info in TryPatchFiles(false))
            {
                yield return info;

                if (info.OK)
                    continue;
                
                // This will run _after_ the caller decides to continue iterating.
                await foreach (var secondInfo in TryPatchFiles(true))
                {
                    yield return secondInfo;
                }

                yield break;
            }
        }

        private string[] GetCorePatches()
        {
            return VFS.GetDirectories(VFS.Combine(GamePath, "SPT_Data/Launcher/Patches/"));
        }

        public ProgressReportingPatchRunner(string GamePath, string[] Patches = null)
        {
            this.GamePath = GamePath;
            this.Patches = Patches ?? GetCorePatches();
        }
    }
}
