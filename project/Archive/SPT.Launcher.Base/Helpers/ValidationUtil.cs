using Microsoft.Win32;
using System.IO;

namespace SPT.Launcher.Helpers
{
    public static class ValidationUtil
    {
        public static bool Validate()
        {
            var c0 = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\EscapeFromTarkov";
            var v0 = 0;

            try
            {
                var v1 = Registry.LocalMachine.OpenSubKey(c0, false).GetValue("InstallLocation");
                var v2 = (v1 != null) ? v1.ToString() : string.Empty;
                var v3 = new DirectoryInfo(v2);
                var v4 = new FileSystemInfo[]
                {
                    v3,
                    new FileInfo(Path.Join(v2, @"BattlEye\BEClient_x64.dll")),
                    new FileInfo(Path.Join(v2, @"BattlEye\BEService_x64.dll")),
                    new FileInfo(Path.Join(v2, "ConsistencyInfo")),
                    new FileInfo(Path.Join(v2, "Uninstall.exe")),
                    new FileInfo(Path.Join(v2, "UnityCrashHandler64.exe"))
                };

                v0 = v4.Length - 1;

                foreach (var value in v4)
                {
                    if (value.Exists)
                    {
                        --v0;
                    }
                }
            }
            catch
            {
                v0 = -1;
            }

            return v0 == 0;
        }
    }
}
