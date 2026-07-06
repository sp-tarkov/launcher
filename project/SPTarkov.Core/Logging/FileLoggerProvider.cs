using Microsoft.Extensions.Logging;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Logging;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path = Paths.LauncherLog;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileLoggerProvider()
    {
        var path = Path.GetDirectoryName(_path);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path!);
        }
        File.Create(_path).Close();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _path, _lock);
    }

    public void Dispose()
    {

    }
}
