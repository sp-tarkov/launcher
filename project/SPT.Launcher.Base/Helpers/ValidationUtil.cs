using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SPT.Launcher.Helpers
{
    public static class ValidationUtil
    {
        static bool b1 = true;
        static string c0 = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\EscapeFromTarkov";
        static string c1 = @"Software\Wow6432Node\Valve\Steam";
        static string c2 = "build";
        static string c3 = "3932890";
        static int v0 = 0;

        public static bool Validate()
        {
            try
            {
                var v1 = a() as object;
                if (v1 == null || !Path.Exists(Path.Combine(v1.ToString(), c2)))
                {
                    b1 = false;
                    v1 = Registry.LocalMachine.OpenSubKey(c0, false).GetValue("InstallLocation");
                }
                var v2 = (v1 != null) ? v1.ToString() : string.Empty;
                v2 = b1 ? Path.Combine(v2, c2) : v2;
                var v3 = new DirectoryInfo(v2);
                var v4 = new FileSystemInfo[]
                {
                    v3,
                    new FileInfo(Path.Join(v2, @"BattlEye\BEClient_x64.dll")),
                    new FileInfo(Path.Join(v2, @"BattlEye\BEService_x64.dll")),
                    new FileInfo(Path.Join(v2, "ConsistencyInfo")),
                    new FileInfo(Path.Join(v2, "UnityPlayer.dll")),
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

        public static string a()
        {
            var c = b();
            if (string.IsNullOrEmpty(c))
                return null;

            var f = d(Path.Combine(c, "steamapps", "libraryfolders.vdf"), "path");
            return f.Length > 0 ? g(f) : null;
        }

        private static string b()
        {
            var h = string.Empty;
            using (var i = Registry.LocalMachine.OpenSubKey(c1, false))
                if (i != null) h = i.GetValue("InstallPath")?.ToString();

            return h;
        }

        private static string g(string[] j)
        {
            var k = $"appmanifest_{c3}.acf";
            foreach (var l in j)
            {
                var m = Path.Combine(l, "steamapps", k);
                if (!File.Exists(m)) continue;

                var n = d(m, "installdir");
                if (n.Length > 0) return Path.Combine(l, "steamapps", "common", n[0]);
            }

            return null;
        }

        private static string[] d(string l, string k)
        {
            if (!File.Exists(l)) return Array.Empty<string>();
            var q = new List<string>();
            var s = $@"""{k}""\s+""(.*)""";
            foreach (var r in File.ReadLines(l))
            {
                var p = Regex.Match(r, s);
                if (p.Success) q.Add(Regex.Unescape(p.Groups[1].Value));
            }

            return q.ToArray();
        }
    }
}
