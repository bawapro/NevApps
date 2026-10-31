using System.Text.RegularExpressions;

namespace NevApps.Services;

public class LoggerService
{
    private static readonly Regex LineNumberRegex = new(@":line\s+(\d+)", RegexOptions.Compiled);

    // Strip machine-specific home dirs from stack traces (C:\Users\name\, /Users/name/, /home/name/).
    private static readonly Regex UserHomePathRegex = new(
        @"([A-Za-z]:\\Users\\[^\\]+\\|/(?:Users|home)/[^/]+/)",
        RegexOptions.Compiled);

    private readonly string _logFilePath;

    public LoggerService() : this(ResolveLogDirectory())
    {
    }

    internal LoggerService(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        _logFilePath = Path.Combine(logDirectory, $"app-log-{today}.txt");
    }

    private static string ResolveLogDirectory()
    {
        try
        {
            return FileSystem.AppDataDirectory;
        }
        catch
        {
            return Path.Combine(Path.GetTempPath(), "NevAppsLogs");
        }
    }

    public void Log(string message, Exception? ex = null, string? extraInfo = null)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var baseInfo = $"[{timestamp}] {message}";

            //NEW: LOG ROTATION CHECK
            var logInfo = new FileInfo(_logFilePath);
            if (logInfo.Exists)
            {
                // 1. MONTHLY PURGE: If the file is from a previous month, delete it
                if (logInfo.LastWriteTime.Month != DateTime.Now.Month ||
                    logInfo.LastWriteTime.Year != DateTime.Now.Year)
                    File.Delete(_logFilePath);
                // 2. SIZE SAFETY: Also check if it's over 2MB just in case
                else if (logInfo.Length > 2 * 1024 * 1024) File.Delete(_logFilePath);
            }

            //USER LOGGING
            var userLine = $"[USER] {baseInfo}";
            if (ex != null) userLine += $" | Issue: {ex.Message}";
            userLine += "\n--------------------------------------------------\n";
            File.AppendAllText(_logFilePath, userLine);

            //DEVELOPER LOGGING
            if (ex != null)
            {
                var match = LineNumberRegex.Match(ex.StackTrace ?? "");
                var lineNumber = match.Success ? match.Groups[1].Value : "Unknown";

                if (!IsDuplicateError(lineNumber))
                {
                    var cleanStack = string.IsNullOrWhiteSpace(ex.StackTrace)
                        ? "(no stack trace)"
                        : UserHomePathRegex.Replace(ex.StackTrace, @"...\");
                    var devLine = $"[DEVELOPER] {baseInfo} | Line: {lineNumber}\n" +
                                  $"Exception: {ex.GetType().Name}: {ex.Message}\n" +
                                  $"StackTrace: {cleanStack}\n" +
                                  "--------------------------------------------------\n";

                    File.AppendAllText(_logFilePath, devLine);
                }
            }
        }
        catch
        {
        }
    }

    private bool IsDuplicateError(string lineNumber)
    {
        if (lineNumber == "Unknown") return false;

        try
        {
            if (!File.Exists(_logFilePath)) return false;

            using var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length < 2) return false;

            var seekPos = Math.Max(0, fs.Length - 2000);
            fs.Seek(seekPos, SeekOrigin.Begin);

            using var reader = new StreamReader(fs);
            var lastLogs = reader.ReadToEnd();

            return lastLogs.Contains($"| Line: {lineNumber}");
        }
        catch
        {
            return false;
        }
    }

    public string GetLogFilePath()
    {
        return _logFilePath;
    }
}