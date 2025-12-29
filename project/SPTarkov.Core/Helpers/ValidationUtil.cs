

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SPTarkov.Core.Helpers;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ValidationUtil
{
    bool b1 = true;
    string c0 = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\EscapeFromTarkov";
    string c1 = @"Software\Wow6432Node\Valve\Steam";
    string c2 = "build";
    string c3 = "3932890";
    int v0 = 0;

    public bool Validate()
    {
        try
        {
            var v1 = a() as object;
            if (v1 == null || !Path.Exists(Path.Join(v1.ToString(), c2)))
            {
                b1 = false;
                v1 = Registry.LocalMachine.OpenSubKey(c0, false).GetValue("InstallLocation");
            }

            var v2 = (v1 != null) ? v1.ToString() : string.Empty;
            v2 = b1 ? Path.Join(v2, c2) : v2;
            var v3 = new DirectoryInfo(v2);
            var v4 = new FileSystemInfo[]
            {
                v3, new FileInfo(Path.Join(v2, @"BattlEye\BEClient_x64.dll")),
                new FileInfo(Path.Join(v2, @"BattlEye\BEService_x64.dll")), new FileInfo(Path.Join(v2, "ConsistencyInfo")),
                new FileInfo(Path.Join(v2, "UnityPlayer.dll")), new FileInfo(Path.Join(v2, "UnityCrashHandler64.exe"))
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

    public string a()
    {
        var c = b();
        if (string.IsNullOrEmpty(c))
            return null;

        var f = d(Path.Join(c, "steamapps", "libraryfolders.vdf"), "path");
        return f.Length > 0 ? g(f) : null;
    }

    private string b()
    {
        var h = string.Empty;
        using (var i = Registry.LocalMachine.OpenSubKey(c1, false))
            if (i != null)
                h = i.GetValue("InstallPath")?.ToString();

        return h;
    }

    private string g(string[] j)
    {
        var k = $"appmanifest_{c3}.acf";
        foreach (var l in j)
        {
            var m = Path.Join(l, "steamapps", k);
            if (!File.Exists(m)) continue;

            var n = d(m, "installdir");
            if (n.Length > 0) return Path.Join(l, "steamapps", "common", n[0]);
        }

        return null;
    }

    private string[] d(string l, string k)
    {
        if (!File.Exists(l)) Array.Empty<string>();
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
