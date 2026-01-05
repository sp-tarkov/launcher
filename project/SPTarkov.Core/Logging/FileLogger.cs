using System.Text;
using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.Logging;

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly SemaphoreSlim _lock;
    private readonly string _path;
    private StringBuilder _sb = new();
    public static LogLevel LogLevel { get; set; } = LogLevel.Information;

    public FileLogger(string name, string path, SemaphoreSlim locker)
    {
        _categoryName = name;
        _path = path;
        _lock = locker;
    }

    public async void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        try
        {
            await _lock.WaitAsync();
            _sb.Clear();
            _sb.Append($"[{_categoryName}]");
            _sb.Append(formatter(state, exception));
            _sb.Append(Environment.NewLine);
            await File.AppendAllTextAsync(_path, _sb.ToString(), Encoding.UTF8);
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
        return logLevel > FileLogger.LogLevel;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }
}
