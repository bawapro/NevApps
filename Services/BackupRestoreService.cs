using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NevApps.Classes;
using NevDBClass.Data;

namespace NevApps.Services;

/// <summary>
///     Represents the metadata for a database backup file.
/// </summary>
public class BackupRestore
{
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public long FileSizeBytes { get; set; }
}

/// <summary>
///     Service for managing SQLite database backups, restorations, and cleanup.
///     Handles file system operations and SQLite connection pooling management.
/// </summary>
public class BackupRestoreService
{
    private readonly string _appDataDirectory;
    private readonly IDbContextFactory<SqLiteDbContext> _dbFactory;
    private readonly LoggerService _logger;

    public BackupRestoreService(IDbContextFactory<SqLiteDbContext> dbContextFactory, LoggerService logger)
        : this(dbContextFactory, logger, ResolveAppDataDirectory())
    {
    }

    internal BackupRestoreService(IDbContextFactory<SqLiteDbContext> dbContextFactory, LoggerService logger,
        string appDataDirectory)
    {
        _dbFactory = dbContextFactory;
        _logger = logger;
        _appDataDirectory = appDataDirectory;
    }

    private static string ResolveAppDataDirectory()
    {
        try
        {
            return FileSystem.AppDataDirectory;
        }
        catch
        {
            return Path.GetTempPath();
        }
    }

    /// <summary>
    ///     Ensures the backup directory exists and returns its full path.
    /// </summary>
    private string GetBackupsFolder()
    {
        var folder = Path.Combine(_appDataDirectory, Constants.BackupFolderName);
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>
    ///     Creates a copy of the current live database.
    ///     Maintains a maximum of 5 backup files, deleting the oldest first.
    /// </summary>
    /// <param name="note">Optional name for the backup file. Defaults to NEVDB.db3.</param>
    /// <returns>A tuple indicating success status and a display message.</returns>
    public async Task<(bool Success, string Message)> CreateBackupAsync(string? note = null)
    {
        try
        {
            var sourceDb = Path.Combine(_appDataDirectory, Constants.DbName);
            var fileName = string.IsNullOrEmpty(note) ? Constants.DbName : note + ".db3";
            var destPath = Path.Combine(GetBackupsFolder(), fileName);

            await ForceCloseDbConnections();

            File.Copy(sourceDb, destPath, true);

            var size = new FileInfo(destPath).Length;

            // Enforce max 5 backups (delete oldest)
            var all = await GetAllBackupsAsync();

            if (all.Count > Constants.MaxBackups)
            {
                var toDelete = all.Take(all.Count - Constants.MaxBackups).ToList();
                foreach (var b in toDelete)
                    if (File.Exists(b.FilePath))
                        File.Delete(b.FilePath);
            }

            return (true, $"Backup created: {fileName.Replace(".db3", "")}");
        }
        catch (Exception ex)
        {
            _logger.Log("Backup failed", ex);
            return (false, "Backup failed.");
        }
    }

    /// <summary>
    ///     Retrieves the 5 most recent backup files from the backup folder.
    /// </summary>
    public async Task<List<BackupRestore>> GetAllBackupsAsync()
    {
        List<BackupRestore>? backups = new();

        try
        {
            var backupFolder = GetBackupsFolder();

            if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);

            var files = Directory.GetFiles(backupFolder, "*.db3").Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime).ToList();

            foreach (var file in files.Take(5))
                backups!.Add(new BackupRestore
                {
                    FileName = file.Name,
                    FilePath = file.FullName,
                    CreatedAt = file.CreationTime,
                    FileSizeBytes = file.Length
                });

            if (files.Count > Constants.MaxBackups)
                foreach (var old in files.Skip(Constants.MaxBackups))
                    try
                    {
                        File.Delete(old.FullName);
                    }
                    catch
                    {
                        /* log */
                    }
        }
        catch (Exception ex)
        {
            _logger.Log("GetAllBackupsAsync", ex);
        }

        return backups ?? new List<BackupRestore>();
    }

    /// <summary>
    ///     Overwrites the live database with a selected backup file or an external stream.
    /// </summary>
    /// <param name="fileName">The name of the file to restore.</param>
    /// <param name="isDownload">True if restoring from an external stream, False if from local storage.</param>
    /// <param name="stream">The source stream if isDownload is true.</param>
    public async Task<(bool Success, string Message)> RestoreBackupAsync(string fileName, bool isDownload,
        Stream? stream)
    {
        try
        {
            await ForceCloseDbConnections();

            if (isDownload)
            {
                await using var dest = File.Create(fileName);
                await stream!.CopyToAsync(dest);
            }
            else
            {
                var backupFolder = GetBackupsFolder();
                var liveDbPath = Path.Combine(_appDataDirectory, Constants.DbName);
                var filePath = Path.Combine(backupFolder, fileName);

                File.Copy(filePath, liveDbPath, true);
            }

            //Implementing missing migrations.
            await ForceCloseDbConnections();
            using var _db = await _dbFactory.CreateDbContextAsync();
            SqliteConnection.ClearAllPools();

            var migrator = _db.GetService<IMigrator>();
            var pendingMigrations = await _db.Database.GetPendingMigrationsAsync();

            foreach (var migration in pendingMigrations)
                try
                {
                    await migrator.MigrateAsync(migration);
                }
                catch (SqliteException ex) when (ex.Message.Contains("already exists"))
                {
                    // Record migration as applied in history table if physical table already existed
                    var efVersion = typeof(DbContext).Assembly
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                        .InformationalVersion ?? "8.0.0";

                    var sql =
                        $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('{migration}', '{efVersion}');";
                    await _db.Database.ExecuteSqlRawAsync(sql);
                }

            return (true, "Database restored successfully.");
        }
        catch (Exception ex)
        {
            _logger.Log("Restore failed", ex);
            return (false, "Restore failed.");
        }
    }

    public void DeleteBackup(string fileName)
    {
        var backupFolder = GetBackupsFolder();
        var filePath = Path.Combine(backupFolder, fileName);
        File.Delete(filePath);
    }

    /// <summary>
    ///     Forcefully releases SQLite file handles and clears memory to prevent "File In Use" errors.
    /// </summary>
    private async Task ForceCloseDbConnections()
    {
        try
        {
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            using var tempDb = await _dbFactory.CreateDbContextAsync();
            await tempDb.Database.OpenConnectionAsync();
            await tempDb.Database.CloseConnectionAsync();
        }
        catch (Exception ex)
        {
            _logger.Log("ForceCloseDbConnections failed (non-fatal)", ex);
        }
    }

    /// <summary>
    ///     Wipes all user data from the database.
    /// </summary>
    /// <exception cref="Exception">Throws if the transaction fails.</exception>
    public async Task ClearEverything()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            await db.ExpenseTrackers.ExecuteDeleteAsync();
            await db.MileageTrackers.ExecuteDeleteAsync();
            await db.Settings.ExecuteDeleteAsync();
            await db.Reminders.ExecuteDeleteAsync();
            await db.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence");

            await transaction.CommitAsync();
            await ForceCloseDbConnections();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.Log("ClearDB", ex);
            throw;
        }
    }
}