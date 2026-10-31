using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NevApps.Classes;
using NevDBClass.Data;
using NevDBClass.Models;

namespace NevApps.Services;

public readonly record struct VehicleMileageSummary(
    string Vehicle,
    int TotalMileage,
    decimal TotalFuelCost,
    decimal TotalFuelFilled,
    decimal FuelMileage);

/// <summary>
///     Service for managing data operations for vehicle mileage and fuel tracking.
///     Handles CRUD operations, search filters, and unit conversions (Metric/Imperial).
/// </summary>
internal class MileageTrackerService
{
    private readonly IDbContextFactory<SqLiteDbContext> _db;
    private readonly LoggerService _loggerService;
    private readonly SettingsService _settings;
    private readonly StateService? _state;

    public MileageTrackerService(IDbContextFactory<SqLiteDbContext> dbFactory, LoggerService logger,
        SettingsService settings, StateService? stateService = null)
    {
        _db = dbFactory;
        _loggerService = logger;
        _settings = settings;
        _state = stateService;
    }

    private Task<List<VehicleMileageSummary>>? _dashboardTask;
    private Task? _warmTask;

    private void MarkDashboardDirty()
    {
        _dashboardTask = null;
        _warmTask = null;
        if (_state is not null)
            _state.DashboardReload = true;
    }

    /// <summary>
    ///     Starts the mileage dashboard read while another tab is on screen.
    /// </summary>
    public void WarmDashboard()
    {
        var load = GetDashboardSummariesAsync();
        if (_warmTask is { IsCompleted: false })
            return;

        _warmTask = WarmImagesAsync(load);
    }

    private async Task WarmImagesAsync(Task<List<VehicleMileageSummary>> load)
    {
        try
        {
            var rows = await load.ConfigureAwait(false);
            await _settings.GetVehicleImagesAsync(rows.Select(r => r.Vehicle)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _loggerService.Log("WarmDashboard", ex);
        }
    }

    /// <summary>
    ///     Retrieves the most recent 10 mileage records based on a fuzzy search across
    ///     start/end locations, details, and vehicle names.
    /// </summary>
    /// <param name="searchString">The keyword to filter by.</param>
    /// <returns>A list of matching <see cref="MileageTracker" /> records.</returns>
    public async Task<List<MileageTracker>> SearchMileageAsync(string searchString)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();

            if (string.IsNullOrWhiteSpace(searchString))
                return await db.MileageTrackers
                    .OrderByDescending(e => e.Date)
                    .ThenByDescending(e => e.Id)
                    .Take(50)
                    .ToListAsync();

            var searchPattern = $"%{searchString}%";

            return await db.MileageTrackers
                .Where(e => EF.Functions.Like(e.Start, searchPattern) ||
                            EF.Functions.Like(e.End, searchPattern) ||
                            EF.Functions.Like(e.Detail, searchPattern) ||
                            EF.Functions.Like(e.Vehicle, searchPattern))
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .Take(50)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in SearchMileageAsync", ex, $"SearchString: {searchString}");
            throw;
        }
    }

    /// <summary>
    ///     Adds a new mileage entry to the database.
    ///     <br /><b>Business Rule:</b> Prevents duplicate entries by checking if the
    ///     same Odometer Start/End already exists for the specific vehicle.
    /// </summary>
    /// <param name="obj">The mileage record to save.</param>
    /// <exception cref="InvalidOperationException">Thrown when a duplicate odometer reading is detected.</exception>
    /// <returns>The unique ID of the newly created record.</returns>
    public async Task<int> AddMileageAsync(MileageTracker obj)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            var exists = await db.MileageTrackers.AnyAsync(m =>
                m.Vehicle == obj.Vehicle && m.OdoStart == obj.OdoStart && m.OdoEnd == obj.OdoEnd);
            var odoExists = await db.MileageTrackers.AnyAsync(m =>
                m.Vehicle == obj.Vehicle && (m.OdoStart == obj.OdoStart || m.OdoEnd == obj.OdoEnd));

            if (exists || odoExists)
                throw new InvalidOperationException(
                    "Cannot enter duplicate entry. Check Odometer readings."
                );

            db.MileageTrackers.Add(obj);
            await db.SaveChangesAsync();
            MarkDashboardDirty();

            return obj.Id;
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in AddMileageAsync", ex, JsonSerializer.Serialize(obj));
            throw;
        }
    }

    /// <summary>
    ///     Retrieves all mileage records from the database using AsNoTracking for performance.
    ///     Ordered by Date and then by ID descending.
    /// </summary>
    public async Task<List<MileageTracker>> GetAllMileagesAsync()
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            return await db.MileageTrackers
                .AsNoTracking()
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetAllMileagesAsync", ex);
            throw;
        }
    }

    /// <summary>
    ///     Updates an existing mileage record in the database.
    /// </summary>
    public async Task<int> UpdateMileageAsync(MileageTracker obj)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            db.MileageTrackers.Update(obj);
            var n = await db.SaveChangesAsync();
            if (n > 0)
                MarkDashboardDirty();
            return n;
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in UpdateMileageAsync", ex, JsonSerializer.Serialize(obj));
            throw;
        }
    }

    /// <summary>
    ///     Deletes a specific mileage record.
    ///     <br /><b>Cleanup:</b> Manually removes any associated records in ExpenseTrackers
    ///     that match the TripId before deleting the mileage entry.
    /// </summary>
    public async Task<int> DeleteMileageAsync(int id)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            var Mileage = await db.MileageTrackers.FindAsync(id);
            var expenseExists = await db.ExpenseTrackers.Where(x => x.TripId == id).AnyAsync();

            if (expenseExists) db.ExpenseTrackers.RemoveRange(db.ExpenseTrackers.Where(x => x.TripId == id));

            if (Mileage != null)
            {
                db.MileageTrackers.Remove(Mileage);
                var n = await db.SaveChangesAsync();
                if (n > 0)
                    MarkDashboardDirty();
                return n;
            }
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in DeleteMileageAsync", ex, $"Id: {id}");
            throw;
        }

        return 0;
    }

    /// <summary>
    ///     Provides a distinct list of the 10 most frequent start locations for UI dropdowns.
    /// </summary>
    public async Task<List<string>> GetStartLocationsAsync(string? value)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();

            var query = db.MileageTrackers.AsNoTracking();

            if (!string.IsNullOrEmpty(value)) query = query.Where(e => e.Start.ToLower().Contains(value.ToLower()));

            return await query
                .Select(e => e.Start)
                .Distinct()
                .OrderBy(e => e)
                .Take(50)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetStartLocationsAsync", ex, $"Value: {value}");
            throw;
        }
    }

    public async Task<List<string>> GetStartLocationsAsync(IReadOnlyCollection<string>? vehicles)
    {
        try
        {
            if (vehicles == null || !vehicles.Any())
                return await GetStartLocationsAsync(string.Empty);

            using var db = await _db.CreateDbContextAsync();
            return await db.MileageTrackers
                .AsNoTracking()
                .Where(e => vehicles.Contains(e.Vehicle))
                .Select(e => e.Start)
                .Distinct()
                .Where(s => !string.IsNullOrEmpty(s))
                .OrderBy(s => s)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetStartsAsync (collection)", ex);
            throw;
        }
    }

    public async Task<List<string>> GetEndLocationsAsync(IReadOnlyCollection<string>? vehicles,
        IReadOnlyCollection<string>? startLocations)
    {
        try
        {
            var hasVehicles = vehicles != null && vehicles.Any();
            var hasStarts = startLocations != null && startLocations.Any();

            if (!hasVehicles && !hasStarts)
                return await GetEndLocationsAsync(string.Empty);

            using var db = await _db.CreateDbContextAsync();

            var query = db.MileageTrackers.AsNoTracking();

            if (hasVehicles)
                query = query.Where(e => vehicles!.Contains(e.Vehicle));

            if (hasStarts)
                query = query.Where(e => startLocations!.Contains(e.Start));

            return await query
                .Where(e => !string.IsNullOrEmpty(e.End))
                .Select(e => e.End.Substring(0, e.End.IndexOf("*")).Trim())
                .Distinct()
                .Where(e => !string.IsNullOrEmpty(e))
                .OrderBy(e => e)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetEndLocationsAsync (Dual Collections)", ex);
            throw;
        }
    }

    public async Task<List<string>> GetGasStationsAsync(IReadOnlyCollection<string>? vehicles)
    {
        try
        {
            if (vehicles == null || !vehicles.Any()) return await GetGasStationsAsync(string.Empty);

            using var db = await _db.CreateDbContextAsync();
            return await db.MileageTrackers
                .AsNoTracking()
                .Where(e => vehicles.Contains(e.Vehicle))
                .Select(e => e.GasStation)
                .Distinct()
                .Where(g => !string.IsNullOrEmpty(g))
                .OrderBy(g => g)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetGasStationsAsync (collection)", ex);
            throw;
        }
    }

    /// <summary>
    ///     Provides a list of the 10 most recently used end locations.
    ///     Sorted by the latest date they were visited.
    /// </summary>
    public async Task<List<string>> GetSavedEndLocationsAsync()
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            return await db.MileageTrackers.AsNoTracking()
                .Where(e => e.End != null && e.End != "")
                .Select(e => e.End)
                .Distinct()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetSavedEndLocationsAsync", ex);
            throw;
        }
    }

    public async Task<List<string>> GetEndLocationsAsync(string? value)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            var query = db.MileageTrackers.AsNoTracking();

            if (!string.IsNullOrEmpty(value)) query = query.Where(e => e.End.ToLower().Contains(value.ToLower()));

            return await query
                .GroupBy(e => e.End)
                .Select(g => new
                {
                    End = g.Key,
                    LatestDate = g.Max(e => e.Date)
                })
                .OrderByDescending(g => g.LatestDate)
                .Select(g => g.End)
                .Take(50)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetEndLocationsAsync", ex, $"Value: {value}");
            throw;
        }
    }

    /// <summary>
    ///     Fetches distinct trip details/notes for UI autocomplete suggestions.
    /// </summary>
    public async Task<List<string?>> GetDetailsAsync(string? value)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();

            var query = db.MileageTrackers.OrderByDescending(o => o.Date).AsNoTracking();

            if (!string.IsNullOrEmpty(value))
                query = query.Where(e => e.Detail != null && e.Detail.ToLower().Contains(value.ToLower()));

            return await query
                .Select(e => e.Detail)
                .Distinct()
                .Take(50)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetDetailsAsync", ex, $"Value: {value}");
            throw;
        }
    }

    /// <summary>
    ///     Retrieves all unique fuel types (e.g., Unleaded, Diesel) used in previous entries.
    /// </summary>
    public async Task<List<string?>> GetFuelTypesAsync()
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            return await db.MileageTrackers
                .AsNoTracking()
                .Select(e => e.FuelType)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetFuelTypesAsync", ex);
            throw;
        }
    }

    /// <summary>
    ///     Provides a distinct, alphabetically sorted list of gas stations from previous entries.
    ///     <br /><b>UX:</b> Used for autocomplete/suggestions when the user is logging a fuel fill-up.
    /// </summary>
    /// <param name="value">The search term to filter gas station names (case-insensitive).</param>
    /// <returns>A list of unique gas station names matching the input.</returns>
    public async Task<List<string?>> GetGasStationsAsync(string? value)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();

            var query = db.MileageTrackers.AsNoTracking();

            if (!string.IsNullOrEmpty(value))
                query = query.Where(e => e.GasStation != null && e.GasStation.ToLower().Contains(value.ToLower()));

            return await query
                .Where(e => !string.IsNullOrEmpty(e.GasStation))
                .Select(e => e.GasStation)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetGasStationsAsync", ex, $"Value: {value}");
            throw;
        }
    }


    /// <summary>
    ///     Calculates the dashboard statistics for a specific vehicle, including current odometer,
    ///     trip distance since the last fill-up, and real-time fuel economy.
    ///     <br /><b>Business Logic:</b>
    ///     <list type="bullet">
    ///         <item>Determines unit system (Imperial vs Metric) from the mileage units setting.</item>
    ///         <item>Calculates <c>fuelMileage</c> as <b>MPG</b> when imperial is on, or <b>L/100km</b> otherwise.</item>
    ///         <item>Aggregates <c>totalDistance</c> by summing all trips occurring <i>after</i> the most recent fuel entry.</item>
    ///         <item>If no vehicle is provided, it defaults to the most recently updated vehicle in the database.</item>
    ///     </list>
    /// </summary>
    /// <param name="vehicle">The name of the vehicle to query. If null, the latest active vehicle is used.</param>
    /// <returns>
    ///     A tuple containing:
    ///     <br />- <b>Latest:</b> The most recent <see cref="MileageTracker" /> record.
    ///     <br />- <b>MileageSinceLastFill:</b> Total distance traveled since the last gas station visit.
    ///     <br />- <b>fuelMileage:</b> Calculated efficiency for the current/incomplete tank.
    ///     <br />- <b>lastFuelMileage:</b> The recorded efficiency from the previous full tank.
    /// </returns>
    public async
        Task<(MileageTracker? Latest, int MileageSinceLastFill, decimal? fuelMileage, decimal? lastFuelMileage)>
        GetLatestRecordAsync(string? vehicle)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            var isImperial = await db.Settings.AnyAsync(x =>
                x.SettingKey == "country" && x.SettingValue.ToLower() == "united states");

            if (string.IsNullOrEmpty(vehicle))
            {
                var record = await db.MileageTrackers
                    .AsNoTracking()
                    .OrderByDescending(e => e.Date)
                    .ThenByDescending(e => e.Id)
                    .FirstOrDefaultAsync();

                vehicle = record?.Vehicle;
            }

            var lastFuelEntry = await db.MileageTrackers
                .AsNoTracking()
                .Where(e => e.Vehicle == vehicle && e.Mileage > 0)
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .Select(e => new { e.Id, e.FuelFilled, e.FuelMileage })
                .FirstOrDefaultAsync();
            if (lastFuelEntry != null)
            {
                var lastMileageId = lastFuelEntry?.Id;

                var lastFuelFilled = lastFuelEntry?.FuelFilled;

                if (lastFuelFilled != null)
                    lastFuelFilled = isImperial
                        ? Math.Round(lastFuelFilled.Value * Constants.Conversions.LitresToUsGallons, 2)
                        : lastFuelFilled;

                var totalDistance = await db.MileageTrackers
                    .AsNoTracking()
                    .Where(e => e.Vehicle == vehicle && e.Id > lastMileageId)
                    .Select(e => e.Distance)
                    .SumAsync();
                if (totalDistance > 0)
                    totalDistance = isImperial
                        ? (int)Math.Round(Constants.Conversions.KmToMiles * totalDistance)
                        : totalDistance;

                var fuelMileage = totalDistance > 0
                    ? isImperial
                        ? Math.Round(totalDistance / lastFuelFilled!.Value, 2)
                        : Math.Round(lastFuelFilled!.Value * 100m / totalDistance, 2)
                    : 0;

                var lastRecord = await db.MileageTrackers
                    .AsNoTracking()
                    .Where(e => e.Vehicle == vehicle)
                    .OrderByDescending(e => e.Date)
                    .ThenByDescending(e => e.Id)
                    .FirstOrDefaultAsync();

                if (lastRecord != null)
                    lastRecord.OdoEnd = isImperial
                        ? (int)Math.Round(Constants.Conversions.KmToMiles * lastRecord.OdoEnd)
                        : lastRecord.OdoEnd;


                return (lastRecord, totalDistance, fuelMileage, lastFuelEntry!.FuelMileage);
            }
            else //When there is no gas entry yet
            {
                var totalDistance = await db.MileageTrackers
                    .AsNoTracking()
                    .Where(e => e.Vehicle == vehicle)
                    .Select(e => e.Distance)
                    .SumAsync();
                totalDistance = isImperial
                    ? (int)Math.Round(Constants.Conversions.KmToMiles * totalDistance)
                    : totalDistance;

                var lastRecord = await db.MileageTrackers
                    .AsNoTracking()
                    .Where(e => e.Vehicle == vehicle)
                    .OrderByDescending(e => e.Date)
                    .ThenByDescending(e => e.Id)
                    .FirstOrDefaultAsync();

                if (lastRecord != null)
                    lastRecord.OdoEnd = isImperial
                        ? (int)Math.Round(Constants.Conversions.KmToMiles * lastRecord.OdoEnd)
                        : lastRecord.OdoEnd;

                return (lastRecord, totalDistance, 0, 0);
            }
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetLatestRecordAsync", ex, $"Vehicle: {vehicle}");
            throw;
        }
    }

    /// <summary>
    ///     Gets the absolute latest odometer reading for a specific vehicle.
    /// </summary>
    public async Task<int> GetLatestOdometerAsync(string vehicle)
    {
        using var db = await _db.CreateDbContextAsync();
        return await db.MileageTrackers
            .AsNoTracking()
            .Where(e => e.Vehicle == vehicle)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .Select(e => e.OdoEnd)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    ///     Gets a list of every vehicle currently present in the database.
    /// </summary>
    public async Task<List<string>> GetAllVehiclesAsync()
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            return await db.MileageTrackers
                .AsNoTracking()
                .Select(e => e.Vehicle)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetAllVehiclesAsync", ex);
            throw;
        }
    }

    /// <summary>
    ///     Gets vehicles ordered by the date of their most recent fuel or energy fill.
    ///     Vehicles without a fill record are placed after vehicles with a fill record.
    /// </summary>
    public async Task<List<string>> GetVehiclesByMostRecentFuelEntryAsync()
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            return await db.MileageTrackers
                .AsNoTracking()
                .GroupBy(e => e.Vehicle)
                .Select(g => new
                {
                    Vehicle = g.Key,
                    LatestFuelDate = g.Where(e => e.FuelFilled > 0)
                        .Select(e => (DateOnly?)e.Date)
                        .Max()
                })
                .OrderByDescending(e => e.LatestFuelDate.HasValue)
                .ThenByDescending(e => e.LatestFuelDate)
                .ThenBy(e => e.Vehicle)
                .Select(e => e.Vehicle)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetVehiclesByMostRecentFuelEntryAsync", ex);
            throw;
        }
    }

    /// <summary>
    ///     Generates a high-level summary for a specific vehicle, totaling costs, distance, and average economy.
    /// </summary>
    /// <summary>
    ///     One grouped read for every vehicle on the mileage dashboard, ordered like
    ///     <see cref="GetVehiclesByMostRecentFuelEntryAsync"/>.
    /// </summary>
    public async Task<List<VehicleMileageSummary>> GetDashboardSummariesAsync()
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            var rows = await db.MileageTrackers
                .AsNoTracking()
                .GroupBy(e => e.Vehicle)
                .Select(g => new
                {
                    Vehicle = g.Key,
                    LatestFuelDate = g.Where(e => e.FuelFilled > 0)
                        .Select(e => (DateOnly?)e.Date)
                        .Max(),
                    TotalMileage = g.Sum(e => e.Distance),
                    TotalFuelCost = g.Where(e => e.FuelFilled > 0).Sum(e => e.FuelPrice) ?? 0,
                    TotalFuelFilled = g.Where(e => e.FuelFilled > 0).Sum(e => e.FuelFilled) ?? 0,
                    FuelMileage = g.Where(e => e.FuelFilled > 0).Average(e => e.FuelMileage) ?? 0
                })
                .ToListAsync();

            return rows
                .OrderByDescending(e => e.LatestFuelDate.HasValue)
                .ThenByDescending(e => e.LatestFuelDate)
                .ThenBy(e => e.Vehicle)
                .Select(e => new VehicleMileageSummary(
                    e.Vehicle,
                    e.TotalMileage,
                    e.TotalFuelCost,
                    e.TotalFuelFilled,
                    Math.Round(e.FuelMileage, 2)))
                .ToList();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetDashboardSummariesAsync", ex);
            throw;
        }
    }

    public async
        Task<(string vehicle, int mileage, decimal totalFuelCost, decimal totalFuelFilled, decimal fuelMileage)>
        GetMileageSummaryAsync(string vehicle)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            var summary = await db.MileageTrackers
                .AsNoTracking()
                .Where(e => e.Vehicle == vehicle)
                .GroupBy(e => e.Vehicle)
                .Select(g => new
                {
                    Vehicle = g.Key,
                    TotalMileage = g.Sum(e => e.Distance),
                    TotalFuelCost = g.Where(e => e.FuelFilled > 0).Sum(e => e.FuelPrice) ?? 0,
                    TotalFuelFilled = g.Where(e => e.FuelFilled > 0).Sum(e => e.FuelFilled) ?? 0,
                    FuelMileage = g.Where(e => e.FuelFilled > 0).Average(e => e.FuelMileage) ?? 0
                })
                .FirstOrDefaultAsync();

            if (summary != null)
                return (summary.Vehicle, summary.TotalMileage, summary.TotalFuelCost, summary.TotalFuelFilled,
                    Math.Round(summary.FuelMileage, 2));
            return (vehicle, 0, 0, 0, 0);
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetMileageSummary", ex, $"Vehicle: {vehicle}");
            throw;
        }
    }
}