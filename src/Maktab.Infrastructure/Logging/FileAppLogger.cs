using Maktab.Application.Abstractions;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Infrastructure.Logging;

public sealed class FileAppLogger(AppFolders folders) : IAppLogger
{
    private static readonly object LogLock = new();

    public void LogInfo(string message)
    {
        WriteLog("INFO", message, null);
    }

    public void LogWarning(string message)
    {
        WriteLog("WARN", message, null);
    }

    public void LogError(string message, Exception? exception = null)
    {
        WriteLog("ERROR", message, exception);
    }

    public async Task<IReadOnlyList<string>> ReadRecentLogsAsync(int maxLines = 100, CancellationToken cancellationToken = default)
    {
        var appLogFile = Path.Combine(folders.Logs, "app.log");
        if (!File.Exists(appLogFile))
        {
            return [];
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(appLogFile, cancellationToken);
            return lines.TakeLast(maxLines).Reverse().ToList();
        }
        catch
        {
            return [];
        }
    }

    private void WriteLog(string level, string message, Exception? exception)
    {
        try
        {
            Directory.CreateDirectory(folders.Logs);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var line = $"[{timestamp}] [{level}] {message}";

            if (exception is not null)
            {
                line += Environment.NewLine + $"Exception: {exception.GetType().FullName}: {exception.Message}" +
                        Environment.NewLine + exception.StackTrace;
            }

            lock (LogLock)
            {
                var appLogFile = Path.Combine(folders.Logs, "app.log");
                File.AppendAllText(appLogFile, line + Environment.NewLine);

                if (level == "ERROR")
                {
                    var errorLogFile = Path.Combine(folders.Logs, "error.log");
                    File.AppendAllText(errorLogFile, line + Environment.NewLine);
                }
            }
        }
        catch
        {
            // Logging should never crash application
        }
    }
}
