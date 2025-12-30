using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.SevenZip;

public interface SevenZip
{
    public ILogger<SevenZip> _logger { get; set; }

    public Task<List<string>> GetEntriesAsync(string pathToZip);

    public Task<bool> ExtractToDirectoryAsync(string pathToZip, string destination);
}
