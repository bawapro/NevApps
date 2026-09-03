using System.Text.RegularExpressions;

namespace NevApps.Services
{
    public class LoggerService
    {
        private readonly string _logFilePath;

        private static readonly Regex LineNumberRegex = new Regex(@":line\s+(\d+)", RegexOptions.Compiled);
        // Strip machine-specific home dirs from stack traces (C:\Users\name\, /Users/name/, /home/name/).
        private static readonly Regex UserHomePathRegex = new(
            @"([A-Za-z]:\\Users\\[^\\]+\\|/(?:Users|home)/[^/]+/)",
            RegexOptions.Compiled);
        public LoggerService()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            _logFilePath = Path.Combine(FileSystem.AppDataDirectory, $"app-log-{today}.txt");
        }

        public void Log(string message, Exception? ex = null, string? extraInfo = null)
        {
            try
            {

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var baseInfo = $"[{timestamp}] {message}";

                var logInfo = new FileInfo(_logFilePath);
                if (logInfo.Exists)
                {
                    // Drop logs from a previous month.
                    if (logInfo.LastWriteTime.Month != DateTime.Now.Month ||
                        logInfo.LastWriteTime.Year != DateTime.Now.Year)
                    {
                        File.Delete(_logFilePath);
                    }
                    // Drop the file if it is larger than 2 MB.
                    else if (logInfo.Length > 2 * 1024 * 1024)
                    {
                        File.Delete(_logFilePath);
                    }
                }

                var userLine = $"[USER] {baseInfo}";
                if (ex != null) userLine += $" | Issue: {ex.Message}";
                userLine += "\n--------------------------------------------------\n";
                File.AppendAllText(_logFilePath, userLine);

                if (ex != null)
                {
                    var match = LineNumberRegex.Match(ex.StackTrace ?? "");
                    string lineNumber = match.Success ? match.Groups[1].Value : "Unknown";

                    if (!string.IsNullOrEmpty(ex.StackTrace) && !IsDuplicateError(lineNumber))
                    {
                        var cleanStack = UserHomePathRegex.Replace(ex.StackTrace, @"...\");
                        var devLine = $"[DEVELOPER] {baseInfo} | Line: {lineNumber}\n" +
                                      $"Exception: {ex.GetType().Name}\n" +
                                      $"StackTrace: {cleanStack}\n" +
                                      "--------------------------------------------------\n";

                        File.AppendAllText(_logFilePath, devLine);
                    }
                }
            }
            catch { }
        }

        private bool IsDuplicateError(string lineNumber)
        {
            if (lineNumber == "Unknown") return false;

            try
            {
                if (!File.Exists(_logFilePath)) return false;

                using var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length < 2) return false;

                long seekPos = Math.Max(0, fs.Length - 2000);
                fs.Seek(seekPos, SeekOrigin.Begin);

                using var reader = new StreamReader(fs);
                string lastLogs = reader.ReadToEnd();

                return lastLogs.Contains($"| Line: {lineNumber}");
            }
            catch { return false; }
        }
    }
}
