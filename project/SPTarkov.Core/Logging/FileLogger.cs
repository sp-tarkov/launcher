using System.Text;
using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.Logging;

public class FileLogger(string name, string path, SemaphoreSlim locker) : ILogger
{
    private readonly StringBuilder _sb = new();
    public static LogLevel LogLevel { get; set; } = LogLevel.Information;

    public async void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        try
        {
            await locker.WaitAsync();
            _sb.Clear();
            _sb.Append($"[{name}]");
            _sb.Append(formatter(state, exception));
            _sb.Append(Environment.NewLine);
            await File.AppendAllTextAsync(path, _sb.ToString(), Encoding.UTF8);
        }
        finally
        {
            locker.Release();
        }
    }

    /// <summary>
    /// Always log.
    /// </summary>
    /// <param name="logLevel"></param>
    /// <returns></returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel > LogLevel;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }
}
