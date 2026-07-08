using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.SevenZip;

public class LinuxSevenZip : SevenZip
{
    public ILogger<SevenZip> Logger { get; set; } = null!;

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
            FileName = Path.Join(Paths.SevenZip, "7zz"),
            WorkingDirectory = Paths.SevenZip,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Arguments = $"l -slt \"{pathToZip}\"",
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

        if (processResult is null)
        {
            throw new InvalidOperationException("Failed to start 7-Zip process");
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
            catch
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
            // launching extraction on a zip is `x -o"Destination" "PathToZip"`
            var process = new ProcessStartInfo
            {
                FileName = Path.Join(Paths.SevenZip, "7zz"),
                WorkingDirectory = Paths.SevenZip,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = $"x -o\"{destination}\"  \"{pathToZip}\"",
            };

            var processResult = Process.Start(process);

            if (processResult is null)
            {
                throw new InvalidOperationException("Failed to start 7-Zip process");
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
                catch
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

    private Task<List<string>> ParseEntries(string outputResult, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        // With the -slt switch, 7-Zip lists the archive's own properties first, then a dashed divider (----------),
        // then one "Path = <name>" line per entry. Skip everything up to the divider so the archive's own path isn't
        // read as an entry.
        var entries = new List<string>();
        var pastArchiveHeader = false;

        foreach (var rawLine in outputResult.Split('\n'))
        {
            token.ThrowIfCancellationRequested();

            var line = rawLine.TrimEnd('\r');

            if (!pastArchiveHeader)
            {
                // The archive-properties separator is "--"; the entry divider is longer.
                if (line.Length >= 5 && line.All(c => c == '-'))
                {
                    pastArchiveHeader = true;
                }

                continue;
            }

            if (line.StartsWith("Path = ", StringComparison.Ordinal))
            {
                entries.Add(line["Path = ".Length..]);
            }
        }

        return Task.FromResult(entries);
    }

    // Example return from 7-Zip with the -slt (technical listing) switch:

    // 7-Zip (z) 25.01 (x64) : Copyright (c) 1999-2025 Igor Pavlov : 2025-08-03
    //
    // Scanning the drive for archives:
    // 1 file, 5860425 bytes (5724 KiB)
    //
    // Listing archive: .../ModCache/fika.ghostfenixx.svm
    //
    // --
    // Path = .../ModCache/fika.ghostfenixx.svm
    // Type = zip
    // Physical Size = 5860425
    //
    // ----------
    // Path = Greed.exe
    // Folder = -
    // Size = 10427448
    // Packed Size = 5771148
    // Modified = 2025-11-11 12:45:52
    // Attributes = A
    //
    // Path = SPT
    // Folder = +
    // Size = 0
    // Packed Size = 0
    //
    // Path = SPT/user/mods/[SVM] Server Value Modifier/Loader/loader.json
    // Folder = -
    // Size = 36
    // Packed Size = 36
}
