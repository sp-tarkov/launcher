using System.Text;
using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.Logging;

public class FileLogger : ILogger
{
    private readonly string categoryName;
    private readonly SemaphoreSlim _lock;
    private readonly string _path;
    private StringBuilder sb = new();

    public FileLogger(string name, string path, SemaphoreSlim locker)
    {
        categoryName = name;
        _path = path;
        _lock = locker;
    }

    public async void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            await _lock.WaitAsync();
            sb.Clear();
            sb.Append($"[{categoryName}]");
            sb.Append(formatter(state, exception));
            sb.Append(Environment.NewLine);
            await File.AppendAllTextAsync(_path, sb.ToString(), Encoding.UTF8);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Always log.
    /// </summary>
    /// <param name="logLevel"></param>
    /// <returns></returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }
}
