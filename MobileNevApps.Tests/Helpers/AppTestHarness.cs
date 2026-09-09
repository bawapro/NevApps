using NevApps.Services;
using NevDBClass.Models;

namespace MobileNevApps.Tests.Helpers;

internal sealed class AppTestHarness : IDisposable
{
    public TestDbContextFactory Db { get; }
    public LoggerService Logger { get; }
    public string TempDir { get; }

    public AppTestHarness()
    {
        TempDir = Path.Combine(Path.GetTempPath(), "MobileNevAppsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TempDir);
        Db = new TestDbContextFactory();
        Logger = new LoggerService(Path.Combine(TempDir, "logs"));
    }

    public ExpenseTrackerService Expenses => new(Db, Logger);
    public MileageTrackerService Mileage => new(Db, Logger);
    public SettingsService Settings => new(Db);
    public ReminderService Reminders => new(Db, Logger);
    public BackupRestoreService Backup => new(Db, Logger, TempDir);

    public static ExpenseTracker Expense(
        string category = "Food",
        string type = "Groceries",
        string place = "Costco",
        decimal price = 12.50m,
        string detail = "Weekly shop",
        DateOnly? date = null,
        int? autoPay = 0,
        DateOnly? autoPayDate = null,
        string? frequency = null,
        int? tripId = null,
        string? tripDestination = null)
        => new()
        {
            Date = date ?? DateOnly.FromDateTime(DateTime.Today),
            Category = category,
            Type = type,
            Place = place,
            Price = price,
            Detail = detail,
            AutoPay = autoPay,
            AutoPayDate = autoPayDate,
            Frequency = frequency,
            TripId = tripId,
            TripDestination = tripDestination
        };

    public static MileageTracker MileageEntry(
        string vehicle = "Civic",
        string start = "Home",
        string end = "Work * Downtown",
        int odoStart = 1000,
        int odoEnd = 1040,
        int distance = 40,
        DateOnly? date = null,
        string? gasStation = null,
        decimal? fuelFilled = null,
        decimal? fuelPrice = null,
        decimal? fuelMileage = null,
        int mileage = 0,
        string? detail = "Commute",
        string? fuelType = "Regular")
        => new()
        {
            Date = date ?? DateOnly.FromDateTime(DateTime.Today),
            Vehicle = vehicle,
            Start = start,
            End = end,
            OdoStart = odoStart,
            OdoEnd = odoEnd,
            Distance = distance,
            Mileage = mileage,
            Detail = detail,
            GasStation = gasStation,
            FuelType = fuelType,
            FuelFilled = fuelFilled,
            FuelPrice = fuelPrice,
            FuelMileage = fuelMileage
        };

    public static Setting Setting(string key, string value, int enabled = 1)
        => new()
        {
            SettingKey = key,
            SettingValue = value,
            IsEnabled = enabled
        };

    public void Dispose()
    {
        Db.Dispose();
        try
        {
            if (Directory.Exists(TempDir))
                Directory.Delete(TempDir, recursive: true);
        }
        catch
        {
            // temp cleanup is best-effort
        }
    }
}
