/* GameStarter.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 * reider123
 */


using SPT.Launcher.Helpers;
using SPT.Launcher.MiniCommon;
using SPT.Launcher.Models.Launcher;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SPT.Launcher.Controllers;
using SPT.Launcher.Interfaces;
using System.Runtime.InteropServices;
using SPT.Launcher.Models.SPT;
using Newtonsoft.Json.Linq;

namespace SPT.Launcher
{
    public class GameStarter
    {
        private readonly IGameStarterFrontend _frontend;
        private readonly bool _showOnly;
        private readonly string _originalGamePath;
        private readonly string[] _excludeFromCleanup;
        private const string steamRegistryInstall = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 3932890";
        private const string registryInstall = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\EscapeFromTarkov";

        public GameStarter(IGameStarterFrontend frontend, string gamePath = null, string originalGamePath = null,
            bool showOnly = false, string[] excludeFromCleanup = null)
        {
            _frontend = frontend;
            _showOnly = showOnly;
            _originalGamePath = originalGamePath ??= DetectOriginalGamePath();
            _excludeFromCleanup = excludeFromCleanup ?? LauncherSettingsProvider.Instance.ExcludeFromCleanup;
        }

        public static string DetectOriginalGamePath()
        {
            // We can't detect the installed path on non-Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return null;

            // We prioritize the Steam version, as the Steam CDN is faster for updates, and if someone
            // owns it on both platforms, there's a better chance their Steam version is up to date
            var steamUninstallValue = Registry.LocalMachine.OpenSubKey(steamRegistryInstall, false)
                ?.GetValue("InstallLocation");
            if (steamUninstallValue != null &&
                steamUninstallValue is string steamUninstallStringValue &&
                Path.Exists(Path.Combine(steamUninstallStringValue, "build")))
            {
                var steamDirInfo = new DirectoryInfo(steamUninstallStringValue);
                var steamBuildDir = Path.Combine(steamDirInfo.FullName, "build");
                return Path.TrimEndingDirectorySeparator(steamBuildDir);
            }

            // Fall back to the BSG Launcher registry key if Steam isn't being used
            var uninstallStringValue = Registry.LocalMachine.OpenSubKey(registryInstall, false)
                ?.GetValue("InstallLocation");
            var info = (uninstallStringValue is string key) ? new DirectoryInfo(key) : null;

            if (info == null)
                return null;

            return Path.TrimEndingDirectorySeparator(info.FullName);
        }

        public async Task<GameStarterResult> LaunchGame(ServerInfo server, AccountInfo account, string gamePath)
        {
            LogManager.Instance.Info(">>> Launching Game");
            LogManager.Instance.Info($">>> Account: {account.username}");
            LogManager.Instance.Info($">>> Server : {server.backendUrl}");
            // setup directories
            if (IsInstalledInLive())
            {
                LogManager.Instance.Error("[LaunchGame] Installed in Live :: YES");
                return GameStarterResult.FromError(-1);
            }

            // Confirm core.dll version matches version server is running
            if (await IsCoreDllVersionMismatched(gamePath))
            {
                LogManager.Instance.Error("[LaunchGame] Core dll mismatch :: FAILED");
                return GameStarterResult.FromError(-8);
            }
            
            LogManager.Instance.Info("[LaunchGame] Installed in Live :: NO");
            
            LogManager.Instance.Info("[LaunchGame] Setup Game Files ...");
            SetupGameFiles(gamePath);

            if (!ValidationUtil.Validate())
            {
                LogManager.Instance.Error("[LaunchGame] Game Validation   :: FAILED");
                return GameStarterResult.FromError(-2);
            }
            
            LogManager.Instance.Info("[LaunchGame] Game Validation   :: OK");

            if (account.wipe)
            {
                LogManager.Instance.Info("[LaunchGame] Wipe profile requested");
                RemoveProfileRegistryKeys(account.id);
                CleanTempFiles();
            }

            // check game path
            var clientExecutable = Path.Join(gamePath, "EscapeFromTarkov.exe");

            if (!File.Exists(clientExecutable))
            {
                LogManager.Instance.Error("[LaunchGame] Valid Game Path   :: FAILED");
                LogManager.Instance.Error($"Could not find {clientExecutable}");
                return GameStarterResult.FromError(-6);
            }
            
            LogManager.Instance.Info("[LaunchGame] Valid Game Path   :: OK");

            // apply patches
            ProgressReportingPatchRunner patchRunner = new ProgressReportingPatchRunner(gamePath);

            try
            {
                await _frontend.CompletePatchTask(patchRunner.PatchFiles());
            }
            catch (TaskCanceledException)
            {
                LogManager.Instance.Error("[LaunchGame] Applying Patch    :: FAILED");
                return GameStarterResult.FromError(-4);
            }
            
            LogManager.Instance.Info("[LaunchGame] Applying Patch    :: OK");
            
            //start game
            var args =
                $"-force-gfx-jobs native -token={account.id} -config={Json.SerializeSingleQuotes(new ClientConfig(server.backendUrl))}";

            if (_showOnly)
            {
                Console.WriteLine($"{clientExecutable} {args}");
                LogManager.Instance.Info("[LaunchGame] NOOP :: show only");
            }
            else
            {
                var clientProcess = new ProcessStartInfo(clientExecutable)
                {
                    Arguments = args,
                    UseShellExecute = false,
                    WorkingDirectory = gamePath,
                };

                try
                {
                    Process.Start(clientProcess);
                    LogManager.Instance.Info("[LaunchGame] Game process started");
                }
                catch (Exception ex)
                {
                    LogManager.Instance.Exception(ex);
                    return GameStarterResult.FromError(-7);
                }
            }

            return GameStarterResult.FromSuccess();
        }

        bool IsInstalledInLive()
        {
            var isInstalledInLive = false;

            try
            {
                var files = new FileInfo[]
                {
                    // SPT files
                    new FileInfo(Path.Combine(_originalGamePath, @"SPT.Launcher.exe")),
                    new FileInfo(Path.Combine(_originalGamePath, @"SPT.Server.exe")),

                    // bepinex files
                    new FileInfo(Path.Combine(_originalGamePath, @"doorstep_config.ini")),
                    new FileInfo(Path.Combine(_originalGamePath, @"winhttp.dll")),

                    // licenses
                    new FileInfo(Path.Combine(_originalGamePath, @"LICENSE-BEPINEX.txt")),
                    new FileInfo(Path.Combine(_originalGamePath, @"LICENSE-ConfigurationManager.txt")),                                    
                    new FileInfo(Path.Combine(_originalGamePath, @"LICENSE-Launcher.txt")),
                    new FileInfo(Path.Combine(_originalGamePath, @"LICENSE-Modules.txt")),
                    new FileInfo(Path.Combine(_originalGamePath, @"LICENSE-Server.txt"))
                };
                var directories = new DirectoryInfo[]
                {
                    new DirectoryInfo(Path.Combine(_originalGamePath, @"SPT_Data")),
                    new DirectoryInfo(Path.Combine(_originalGamePath, @"BepInEx"))
                };

                foreach (var file in files)
                {
                    if (File.Exists(file.FullName))
                    {
                        File.Delete(file.FullName);
                        LogManager.Instance.Warning($"File removed :: found in live dir: {file.FullName}");
                        isInstalledInLive = true;
                    }
                }

                foreach (var directory in directories)
                {
                    if (Directory.Exists(directory.FullName))
                    {
                        RemoveFilesRecurse(directory);
                        LogManager.Instance.Warning($"Directory removed :: found in live dir: {directory.FullName}");
                        isInstalledInLive = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.Exception(ex);
            }

            return isInstalledInLive;
        }

        static async Task<bool> IsCoreDllVersionMismatched(string gamePath)
        {
            try
            {
                var serverVersion = new SPTVersion(await ServerManager.GetVersionAsync());

                var coreDllVersionInfo = FileVersionInfo.GetVersionInfo(Path.Join(gamePath, @"\BepinEx\plugins\spt", "spt-core.dll"));
                var dllVersion = new SPTVersion(coreDllVersionInfo.FileVersion);

                LogManager.Instance.Info($"[LaunchGame] spt-core.dll version: {dllVersion}");

                // Edge case, running on locally built modules dlls, ignore check and return ok
                if (dllVersion.Major == 1) return false;

                // check 'X'.x.x
                if (serverVersion.Major != dllVersion.Major) return true;

                // check x.'X'.x
                if (serverVersion.Minor != dllVersion.Minor) return true;

                // check x.x.'X'
                if (serverVersion.Build != dllVersion.Build) return true;

                return false; // Versions match, hooray
            }
            catch (Exception ex)
            {
                LogManager.Instance.Exception(ex);
            }

            return true;
        }

        void SetupGameFiles(string gamePath)
        {
            var files = new []
            {
                GetFileForCleanup("BattlEye", gamePath),
                GetFileForCleanup("Logs", gamePath),
                GetFileForCleanup("ConsistencyInfo", gamePath),
                GetFileForCleanup("EscapeFromTarkov_BE.exe", gamePath),
                GetFileForCleanup("Uninstall.exe", gamePath),
                GetFileForCleanup("UnityCrashHandler64.exe", gamePath),
                GetFileForCleanup("WinPixEventRuntime.dll", gamePath),
                
                // Don't allow excluding this from cleanup ever
                Path.Combine(gamePath, @"EscapeFromTarkov_Data\Plugins\x86_64\hwecho.dll"),
            };

            foreach (var file in files)
            {
                if (file == null)
                {
                    continue;
                }

                if (Directory.Exists(file))
                {
                    RemoveFilesRecurse(new DirectoryInfo(file));
                }

                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        private string GetFileForCleanup(string fileName, string gamePath)
        {
            if (_excludeFromCleanup.Contains(fileName))
            {
                LogManager.Instance.Info($"Excluded {fileName} from file cleanup");
                return null;
            }
            
            return Path.Combine(gamePath, fileName);
        }

        /// <summary>
        /// Remove the SPT JSON-based registry keys associated with the given profile ID
        /// </summary>
		public void RemoveProfileRegistryKeys(string profileId)
        {
            var registryFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, "user\\sptRegistry\\registry.json"));

            if (!registryFile.Exists)
            {
                return;
            }

            JObject registryData = JObject.Parse(File.ReadAllText(registryFile.FullName));

            // Find any property that has a key containing the profileId, and remove it
            var propsToRemove = registryData.Properties().Where(prop => prop.Name.Contains(profileId, StringComparison.CurrentCultureIgnoreCase)).ToList();
            propsToRemove.ForEach(prop => prop.Remove());

            File.WriteAllText(registryFile.FullName, registryData.ToString());
        }

        /// <summary>
        /// Clean the temp folder
        /// </summary>
        /// <returns>returns true if the temp folder was cleaned succefully or doesn't exist. returns false if something went wrong.</returns>
		public bool CleanTempFiles()
        {
            var rootdir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "user\\sptappdata"));

            if (!rootdir.Exists)
            {
                return true;
            }

            return RemoveFilesRecurse(rootdir);
        }

        bool RemoveFilesRecurse(DirectoryInfo basedir)
        {
            LogManager.Instance.Info($"Recursive Removal: {basedir}");
            
            if (!basedir.Exists)
            {
                return true;
            }

            try
            {
                // remove subdirectories
                foreach (var dir in basedir.EnumerateDirectories())
                {
                    RemoveFilesRecurse(dir);
                }

                // remove files
                var files = basedir.GetFiles();

                foreach (var file in files)
                {
                    file.IsReadOnly = false;
                    file.Delete();
                }

                // remove directory
                basedir.Delete();
            }
            catch (Exception ex)
            {
                LogManager.Instance.Exception(ex);
                return false;
            }

            return true;
        }
    }
}
