namespace SPTarkov.Core.SevenZip;

public interface SevenZip
{
    public string? PathToSevenZip { get; set; }

    public Task<List<string>> GetEntriesAsync(string pathToZip);

    public Task<bool> ExtractToDirectoryAsync(string pathToZip, string destination);
}
