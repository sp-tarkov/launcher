namespace SPTarkov.Core.Helpers;

// TODO: change over to Microsoft.Extensions.Logging
// maybe even see if the servers logger can be used?
public class LogHelper
{
    private readonly Lock _lock = new();
    public List<string> Logs = new();

    public void AddLog(string log)
    {
        lock (_lock)
        {
            Logs.Add(log);
        }
    }

    public List<string> GetLogs()
    {
        return Logs;
    }

    public void LogInfo(string log)
    {
        AddLog($"[INFO] {log}");
    }

    public void LogError(string log)
    {
        AddLog($"[ERROR] {log}");
    }

    public void LogWarning(string log)
    {
        AddLog($"[WARNING] {log}");
    }
}
