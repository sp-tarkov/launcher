using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.SevenZip;

public interface SevenZip
{
    public ILogger<SevenZip> Logger { get; set; }

    public Task<List<string>> GetEntriesAsync(string pathToZip, CancellationToken token);

    public Task<bool> ExtractToDirectoryAsync(string pathToZip, string destination, CancellationToken token);
}
