/* LogManager.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


using System;
using System.IO;
using SPT.Launcher.Helpers;

namespace SPT.Launcher.Controllers
{
    /// <summary>
    /// LogManager
    /// </summary>
    public class LogManager
    {
        private static LogManager _instance;
        public static LogManager Instance => _instance ??= new LogManager();
        private readonly string _filePath;
        public readonly string LogFile;

        private LogManager()
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user", "logs");
            LogFile = Path.Combine(_filePath, "launcher.log");

            if (File.Exists(LogFile))
            {
                File.Delete(LogFile);
            }
            
            Write($" ==== Launcher Started ====");
        }

        private string GetDevModeTag()
        {
            if (LauncherSettingsProvider.Instance != null && LauncherSettingsProvider.Instance.IsDevMode)
            {
                return "[DEV_MODE]";
            }

            return "";
        }

        private void Write(string text)
        {
            if (!Directory.Exists(_filePath))
            {
                Directory.CreateDirectory(_filePath);
            }
            
            File.AppendAllLines(LogFile, new[] { $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]{GetDevModeTag()}{text}" });
        }

        public void Debug(string text) => Write($"[Debug] {text}");

        public void Info(string text) => Write($"[Info] {text}");

        public void Warning(string text) => Write($"[Warning] {text}");

        public void Error(string text) => Write($"[Error] {text}");

        public void Exception(Exception ex) => Write($"[Exception] {ex.Message}\nStacktrace:\n{ex.StackTrace}");
    }
}
