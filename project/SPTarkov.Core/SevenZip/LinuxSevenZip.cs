using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.SevenZip;

public class LinuxSevenZip : SevenZip
{
    public ILogger<SevenZip> _logger { get; set; }

    public async Task<List<string>> GetEntriesAsync(string pathToZip)
    {
        if (Paths.SevenZip is null)
        {
            throw new ArgumentNullException(nameof(Paths.SevenZip));
        }

        if (pathToZip is null)
        {
            throw new ArgumentNullException(nameof(pathToZip));
        }

        var process = new ProcessStartInfo
        {
            FileName = Path.Join(Paths.SevenZip, "7zz"),
            WorkingDirectory = Paths.SevenZip,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Arguments = $"l \"{pathToZip}\"",
        };

        Process? processResult;

        try
        {
            processResult = Process.Start(process);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e.Message);
            throw;
        }

        var output = processResult.StandardOutput.ReadToEnd();
        var error = processResult.StandardError.ReadToEnd();

        await processResult.WaitForExitAsync();

        if (!string.IsNullOrEmpty(error))
        {
            throw new Exception(error);
        }

        return await ParseEntries(output);
    }

    public async Task<bool> ExtractToDirectoryAsync(string pathToZip, string destination)
    {
        if (Paths.SevenZip is null)
        {
            throw new ArgumentNullException(nameof(Paths.SevenZip));
        }

        if (pathToZip is null)
        {
            throw new ArgumentNullException(nameof(pathToZip));
        }

        if (destination is null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

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
                Arguments = $"x -o\"{destination}\"  \"{pathToZip}\" ",
            };

            var processResult = Process.Start(process);

            var output = processResult.StandardOutput.ReadToEnd();
            var error = processResult.StandardError.ReadToEnd();

            await processResult.WaitForExitAsync();

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception(error);
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical("Exception occured while extracting to directory: {e}", e);
            return false;
        }

        return true;
    }

    private async Task<List<string>> ParseEntries(string outputResult)
    {
        // split on ------------------------ to remove the first part that isnt needed
        // this will also split the ------------------------ of the end, so we want further work to happen to the middle section [1]
        var afterSplit = outputResult.Split("------------------------");
        // now split on \n this should return multiple lines
        var afterSplit2 = afterSplit[1].Split("\n");

        var listOfEntries = new List<string>();
        var sb = new StringBuilder();

        foreach (var s in afterSplit2)
        {
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
}
