using NevApps.Classes;
using MobileNevApps.Tests.Helpers;

namespace MobileNevApps.Tests.Services;

public class BackupRestoreServiceTests : IDisposable
{
    private readonly AppTestHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private void WriteLiveDb(string contents = "live-db")
        => File.WriteAllText(Path.Combine(_harness.TempDir, Constants.DbName), contents);

    [Fact]
    public async Task CreateBackupAsync_CopiesLiveDatabase()
    {
        WriteLiveDb("backup-me");
        var (success, message) = await _harness.Backup.CreateBackupAsync("first");

        Assert.True(success);
        Assert.Contains("first", message);
        var backups = await _harness.Backup.GetAllBackupsAsync();
        Assert.Single(backups);
        Assert.Equal("backup-me", File.ReadAllText(backups[0].FilePath));
    }

    [Fact]
    public async Task CreateBackupAsync_KeepsOnlyFiveNewest()
    {
        WriteLiveDb();
        for (int i = 0; i < 7; i++)
            await _harness.Backup.CreateBackupAsync($"b{i}");

        var backups = await _harness.Backup.GetAllBackupsAsync();
        var files = Directory.GetFiles(Path.Combine(_harness.TempDir, Constants.BackupFolderName), "*.db3");
        Assert.True(files.Length <= Constants.MaxBackups);
        Assert.True(backups.Count <= Constants.MaxBackups);
    }

    [Fact]
    public async Task RestoreBackupAsync_OverwritesLiveDatabase()
    {
        WriteLiveDb("original");
        await _harness.Backup.CreateBackupAsync("snap");
        File.WriteAllText(Path.Combine(_harness.TempDir, Constants.DbName), "changed");

        var (success, message) = await _harness.Backup.RestoreBackupAsync("snap.db3");
        Assert.True(success);
        Assert.Contains("restored", message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("original", File.ReadAllText(Path.Combine(_harness.TempDir, Constants.DbName)));
    }

    [Fact]
    public void DeleteBackup_RemovesFile()
    {
        var folder = Path.Combine(_harness.TempDir, Constants.BackupFolderName);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "gone.db3");
        File.WriteAllText(path, "x");

        _harness.Backup.DeleteBackup("gone.db3");
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task ClearEverything_RemovesAllTables()
    {
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense());
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry());
        await _harness.Settings.SaveSettingsAsync(AppTestHarness.Setting("country", "Canada"));
        await _harness.Reminders.AddReminderAsync(new NevDBClass.Models.Reminder
        {
            Name = "Test",
            Type = NevDBClass.Models.Reminder.ReminderType.Birthday,
            Date = DateOnly.FromDateTime(DateTime.Today)
        });

        await _harness.Backup.ClearEverything();

        Assert.Empty(await _harness.Expenses.GetAllExpensesAsync());
        Assert.Empty(await _harness.Mileage.GetAllMileagesAsync());
        Assert.Empty(await _harness.Settings.GetSettingsAsync());
        Assert.Empty(await _harness.Reminders.GetAllRemindersAsync());
    }
}
