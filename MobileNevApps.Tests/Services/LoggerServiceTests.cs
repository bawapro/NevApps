using NevApps.Services;

namespace MobileNevApps.Tests.Services;

public class LoggerServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "MobileNevAppsLogsTests", Guid.NewGuid().ToString("N"));

    public LoggerServiceTests()
    {
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Log_WritesUserLineToFile()
    {
        var logger = new LoggerService(_dir);
        logger.Log("hello world");

        Assert.True(File.Exists(logger.GetLogFilePath()));
        var contents = File.ReadAllText(logger.GetLogFilePath());
        Assert.Contains("[USER]", contents);
        Assert.Contains("hello world", contents);
    }

    [Fact]
    public void Log_Exception_WritesDeveloperSectionOncePerLine()
    {
        var logger = new LoggerService(_dir);
        Exception ex;
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (Exception caught)
        {
            ex = caught;
        }

        logger.Log("failed", ex, "extra");
        logger.Log("failed again", ex);

        var contents = File.ReadAllText(logger.GetLogFilePath());
        Assert.Contains("[DEVELOPER]", contents);
        Assert.Contains("InvalidOperationException", contents);
        Assert.Equal(1, CountOccurrences(contents, "[DEVELOPER]"));
    }

    [Fact]
    public void GetLogFilePath_UsesTodayStamp()
    {
        var logger = new LoggerService(_dir);
        Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), logger.GetLogFilePath());
        Assert.StartsWith(_dir, logger.GetLogFilePath());
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}
