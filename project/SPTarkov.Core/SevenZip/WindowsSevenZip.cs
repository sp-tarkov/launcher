using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.SevenZip;

public class WindowsSevenZip : SevenZip
{
    public ILogger<SevenZip> Logger { get; set; }

    public async Task<List<string>> GetEntriesAsync(string pathToZip, CancellationToken token)
    {
        if (Paths.SevenZip is null)
        {
            throw new ArgumentNullException(nameof(Paths.SevenZip));
        }
        if (pathToZip is null)
        {
            throw new ArgumentNullException(nameof(pathToZip));
        }

        token.ThrowIfCancellationRequested();

        var process = new ProcessStartInfo
        {
            FileName = Path.Join(Paths.SevenZip, "7za.exe"),
            WorkingDirectory = Paths.SevenZip,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow =  true,
            Arguments = $"l \"{pathToZip}\"",
        };

        Process? processResult;

        try
        {
            processResult = Process.Start(process);
        }
        catch (Exception e)
        {
            Logger.LogCritical(e.Message);
            throw;
        }

        // register killing the process if the user cancels
        using var registration = token.Register(() =>
        {
            try
            {
                if (!processResult.HasExited)
                {
                    processResult.Kill(entireProcessTree: true);
                }
            }
            catch (Exception _)
            {
                // ignored
            }
        });

        var output = await processResult.StandardOutput.ReadToEndAsync(token);
        var error = await processResult.StandardError.ReadToEndAsync(token);

        await processResult.WaitForExitAsync(token);

        if (!string.IsNullOrEmpty(error))
        {
            throw new Exception(error);
        }

        return await ParseEntries(output, token);
    }

    public async Task<bool> ExtractToDirectoryAsync(string pathToZip, string destination, CancellationToken token)
    {
        if (Paths.SevenZip is null)
        {
            throw new ArgumentNullException(nameof(Paths.SevenZip));
        }

        if (string.IsNullOrEmpty(pathToZip))
        {
            throw new ArgumentNullException(nameof(pathToZip));
        }

        if (string.IsNullOrEmpty(destination))
        {
            throw new ArgumentNullException(nameof(destination));
        }

        token.ThrowIfCancellationRequested();

        try
        {
            var process = new ProcessStartInfo
            {
                FileName = Path.Join(Paths.SevenZip, "7za.exe"),
                WorkingDirectory = Paths.SevenZip,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow =  true,
                Arguments = $"x -o\"{destination}\"  \"{pathToZip}\"",
            };

            var processResult = Process.Start(process);

            // register killing the process if the user cancels
            using var registration = token.Register(() =>
            {
                try
                {
                    if (!processResult.HasExited)
                    {
                        processResult.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception _)
                {
                    // ignored
                }
            });

            var output = await processResult.StandardOutput.ReadToEndAsync(token);
            var error = await processResult.StandardError.ReadToEndAsync(token);

            await processResult.WaitForExitAsync(token);

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception(error);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Exception occured while extracting to directory: {e}", e);
            return false;
        }

        return true;
    }

    /// <summary>
    /// TODO: maybe a regex wizard can do this better
    /// </summary>
    /// <param name="outputResult"></param>
    /// <returns></returns>
    private async Task<List<string>> ParseEntries(string outputResult, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        // split on ------------------------ to remove the first part that isnt needed
        // this will also split the ------------------------ of the end, so we want further work to happen to the middle section [1]
        var afterSplit = outputResult.Split("------------------------");

        // now split on \r\n this should return multiple lines
        var afterSplit2 = afterSplit[1].Split("\r\n");

        var listOfEntries = new List<string>();
        var sb = new StringBuilder();

        foreach (var s in afterSplit2)
        {
            token.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(s.Trim()) || s.Contains("-------------------"))
            {
                continue;
            }

            var counter = 1;
            var lastCharacter = '1';
            var readingEntry = false;

            // entry is always 6th - This was a lie, check comments below method :sadge:
            foreach (var c in s)
            {
                if (c == ' ' && lastCharacter != ' ' && !readingEntry)
                {
                    counter++;
                    lastCharacter = c;
                    continue;
                }

                // if c is not a ' ' after a ' ' and isnt a number, we have a drakia zip :madge: make count 6
                if (c != ' ' && lastCharacter == ' ' && counter == 5 && !int.TryParse(c.ToString(), out var _))
                {
                    counter = 6;
                }

                if (c == ' ' && lastCharacter == ' ' && !readingEntry)
                {
                    lastCharacter = c;
                    continue;
                }

                if (counter == 6)
                {
                    if (!readingEntry)
                    {
                        readingEntry = true;
                    }

                    // hopefully the rest is the entry
                    sb.Append(c);
                }

                lastCharacter = c;
            }

            var stb = sb.ToString();
            listOfEntries.Add(stb);
            sb.Clear();
        }

        return listOfEntries;
    }

    // Example return from 7-zip
    // 7-Zip (a) 25.01 (x64) : Copyright (c) 1999-2025 Igor Pavlov : 2025-08-03
    // Scanning the drive for archives:
    // 1 file, 5860425 bytes (5724 KiB)
    //
    // Listing archive: C:\Repos\launcher\project\SPTarkov.Launcher\bin\Debug\net10.0\user\Launcher\ModCache\fika.ghostfenixx.svm
    //
    // --
    // Path = C:\Repos\launcher\project\SPTarkov.Launcher\bin\Debug\net10.0\user\Launcher\ModCache\fika.ghostfenixx.svm
    //     Type = zip
    // Physical Size = 5860425
    //
    // Date      Time    Attr         Size   Compressed  Name
    // ------------------- ----- ------------ ------------  ------------------------
    // 2025-11-11 12:45:52 ....A     10427448      5771148  Greed.exe
    // 2025-10-12 04:11:57 D....            0            0  SPT
    // 2025-10-12 04:12:08 D....            0            0  SPT\user
    // 2025-10-12 04:12:14 D....            0            0  SPT\user\mods
    // 2025-10-12 21:20:53 D....            0            0  SPT\user\mods\[SVM] Server Value Modifier
    // 2025-10-12 11:09:42 D....            0            0  SPT\user\mods\[SVM] Server Value Modifier\Loader
    // 2025-10-27 13:51:48 ....A           36           36  SPT\user\mods\[SVM] Server Value Modifier\Loader\loader.json
    // 2025-10-12 21:22:28 D....            0            0  SPT\user\mods\[SVM] Server Value Modifier\Misc
    // 2025-10-11 23:49:49 ....A         5448         3002  SPT\user\mods\[SVM] Server Value Modifier\Misc\MOTD.txt
    // 2025-01-04 06:33:56 ....A         4866          496  SPT\user\mods\[SVM] Server Value Modifier\Misc\Waves.json
    // 2025-10-12 11:10:29 D....            0            0  SPT\user\mods\[SVM] Server Value Modifier\Presets
    // 2025-11-11 12:44:22 ....A       239104        83455  SPT\user\mods\[SVM] Server Value Modifier\ServerValueModifier.dll
    // ------------------- ----- ------------ ------------  ------------------------
    // 2025-11-11 12:45:52           10676902      5858137  5 files, 7 folders

    // Path = C:\Repos\launcher\project\SPTarkov.Launcher\bin\Debug\net10.0\user\Launcher\ModCache\xyz.drakia.waypoints
    // Type = 7z
    // Physical Size = 50345268
    // Headers Size = 580
    // Method = LZMA2:25 BCJ
    // Solid = +
    // Blocks = 2
    //
    //    Date      Time    Attr         Size   Compressed  Name
    // ------------------- ----- ------------ ------------  ------------------------
    // 2023-04-03 02:47:46 D....            0            0  BepInEx
    // 2025-11-02 21:19:31 D....            0            0  BepInEx\plugins
    // 2024-07-06 21:48:46 D....            0            0  BepInEx\plugins\DrakiaXYZ-Waypoints
    // 2025-11-03 00:20:52 D....            0            0  BepInEx\plugins\DrakiaXYZ-Waypoints\navmesh
    // 2023-04-03 02:37:55 ....A         1088     50332172  BepInEx\plugins\DrakiaXYZ-Waypoints\LICENSE
    // 2025-11-02 22:28:46 ....A      3436848               BepInEx\plugins\DrakiaXYZ-Waypoints\navmesh\bigmap-navmesh.bundle
    // 2025-11-02 23:20:37 ....A       369289               BepInEx\plugins\DrakiaXYZ-Waypoints\navmesh\factory4-navmesh.bundle
    // 2025-10-29 01:54:06 ....A      3955458               BepInEx\plugins\DrakiaXYZ-Waypoints\navmesh\interchange-navmesh.bundle
    // 2025-10-29 02:20:02 ....A       501619               BepInEx\plugins\DrakiaXYZ-Waypoints\navmesh\laboratory-navmesh.bundle
    // 2025-10-29 02:44:04 ....A        81850               BepInEx\plugins\DrakiaXYZ-Waypoints\navmesh\labyrinth-navmesh.bundle
    // 2025-10-29 03:53:50 ....A     11274305               BepInEx\plugins\DrakiaXYZ-Waypoints\navmesh\lighthouse-navmesh.bundle
    // 2025-10-29 04:27:45 ....A      2667864               BepInEx\plugins\DrakiaXYZ-Waypoints\navmesh\rezervbase-navmesh.bundle
    // 2025-11-02 23:48:36 ....A      1342236               BepInEx\plugins\DrakiaXYZ-Waypoints\navmesh\sandbox-navmesh.bundle
    // 2025-11-03 00:08:37 ....A     11917446               BepInEx\plugins\DrakiaXYZ-Waypoints\navmesh\shoreline-navmesh.bundle
    // 2025-11-02 20:17:43 ....A      4345000               BepInEx\plugins\DrakiaXYZ-Waypoints\navmesh\tarkovstreets-navmesh.bundle
    // 2025-11-03 00:20:10 ....A     10436051               BepInEx\plugins\DrakiaXYZ-Waypoints\navmesh\woods-navmesh.bundle
    // 2025-11-03 04:42:25 ....A        29696        12516  BepInEx\plugins\DrakiaXYZ-Waypoints\DrakiaXYZ-Waypoints.dll
    // ------------------- ----- ------------ ------------  ------------------------
    // 2025-11-03 04:42:25           50358750     50344688  13 files, 4 folders
}
