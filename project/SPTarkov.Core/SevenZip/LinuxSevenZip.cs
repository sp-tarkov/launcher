namespace SPTarkov.Core.SevenZip;

public class LinuxSevenZip
{
    public string PathToSevenZip { get; set; }

    public async Task<List<string>> GetEntriesAsync(string pathToZip)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ExtractToDirectoryAsync(string pathToZip, string destination)
    {
        throw new NotImplementedException();
    }
}
