using MobileNevApps.Tests.Helpers;

namespace MobileNevApps.Tests.Services;

public class MileageTrackerServiceTests : IDisposable
{
    private readonly AppTestHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task AddAndGetAllMileages_ReturnsNewestFirst()
    {
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(date: new DateOnly(2026, 1, 1), odoStart: 100, odoEnd: 140));
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(date: new DateOnly(2026, 2, 1), odoStart: 140, odoEnd: 180));

        var all = await _harness.Mileage.GetAllMileagesAsync();
        Assert.Equal(2, all.Count);
        Assert.Equal(new DateOnly(2026, 2, 1), all[0].Date);
    }

    [Fact]
    public async Task AddMileageAsync_DuplicateOdometer_Throws()
    {
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(odoStart: 1000, odoEnd: 1040));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(odoStart: 1000, odoEnd: 1080, start: "A", end: "B * X")));
    }

    [Fact]
    public async Task SearchMileageAsync_MatchesStartEndDetailAndVehicle()
    {
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(vehicle: "Civic", start: "Home", end: "Airport * YYZ", detail: "Flight"));
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(vehicle: "Tesla", start: "Office", end: "Plant * Fremont", odoStart: 2000, odoEnd: 2100));

        var byVehicle = await _harness.Mileage.SearchMileageAsync("tesla");
        var empty = await _harness.Mileage.SearchMileageAsync(" ");

        Assert.Single(byVehicle);
        Assert.Equal("Tesla", byVehicle[0].Vehicle);
        Assert.Equal(2, empty.Count);
    }

    [Fact]
    public async Task UpdateMileageAsync_PersistsChanges()
    {
        var entry = AppTestHarness.MileageEntry();
        var id = await _harness.Mileage.AddMileageAsync(entry);
        entry.Id = id;
        entry.Detail = "Updated";

        await _harness.Mileage.UpdateMileageAsync(entry);
        var saved = (await _harness.Mileage.GetAllMileagesAsync()).Single();
        Assert.Equal("Updated", saved.Detail);
    }

    [Fact]
    public async Task DeleteMileageAsync_RemovesLinkedExpenses()
    {
        var id = await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry());
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(tripId: id, tripDestination: "Trip"));

        Assert.Equal(2, await _harness.Mileage.DeleteMileageAsync(id));
        Assert.Empty(await _harness.Mileage.GetAllMileagesAsync());
        Assert.Empty(await _harness.Expenses.GetAllExpensesAsync());
        Assert.Equal(0, await _harness.Mileage.DeleteMileageAsync(id));
    }

    [Fact]
    public async Task DeleteAllMileagesAsync_ClearsTable()
    {
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry());
        await _harness.Mileage.DeleteAllMileagesAsync();
        Assert.Empty(await _harness.Mileage.GetAllMileagesAsync());
    }

    [Fact]
    public async Task LocationAndStationLookups_FilterByVehicle()
    {
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(
            vehicle: "Civic", start: "Home", end: "Work * Downtown", gasStation: "Petro"));
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(
            vehicle: "Tesla", start: "Garage", end: "Office * Midtown", odoStart: 5000, odoEnd: 5050, gasStation: "ChargePoint"));

        var starts = await _harness.Mileage.GetStartLocationsAsync(new[] { "Civic" });
        var ends = await _harness.Mileage.GetEndLocationsAsync(new[] { "Civic" }, new[] { "Home" });
        var stations = await _harness.Mileage.GetGasStationsAsync(new[] { "Civic" });
        var fuelTypes = await _harness.Mileage.GetFuelTypesAsync();

        Assert.Equal(new[] { "Home" }, starts);
        Assert.Equal(new[] { "Work" }, ends);
        Assert.Equal(new[] { "Petro" }, stations);
        Assert.Contains("Regular", fuelTypes);
    }

    [Fact]
    public async Task GetLatestOdometerAndVehicles()
    {
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(vehicle: "Civic", odoStart: 100, odoEnd: 150, date: new DateOnly(2026, 1, 1)));
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(vehicle: "Civic", odoStart: 150, odoEnd: 200, date: new DateOnly(2026, 2, 1)));

        Assert.Equal(200, await _harness.Mileage.GetLatestOdometerAsync("Civic"));
        Assert.Equal(new[] { "Civic" }, await _harness.Mileage.GetAllVehiclesAsync());
    }

    [Fact]
    public async Task GetMileageSummaryAsync_TotalsDistanceAndFuel()
    {
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(
            odoStart: 100, odoEnd: 200, distance: 100, fuelFilled: 10, fuelPrice: 20, fuelMileage: 8));
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(
            odoStart: 200, odoEnd: 260, distance: 60, fuelFilled: 6, fuelPrice: 12, fuelMileage: 10));

        var (vehicle, mileage, cost, filled, avg) = await _harness.Mileage.GetMileageSummaryAsync("Civic");

        Assert.Equal("Civic", vehicle);
        Assert.Equal(160, mileage);
        Assert.Equal(32m, cost);
        Assert.Equal(16m, filled);
        Assert.Equal(9m, avg);
    }

    [Fact]
    public async Task GetLatestRecordAsync_Metric_UsesLitresPer100Km()
    {
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(
            odoStart: 1000, odoEnd: 1100, distance: 100, mileage: 1, fuelFilled: 8m, gasStation: "Shell", date: new DateOnly(2026, 1, 1)));
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(
            odoStart: 1100, odoEnd: 1300, distance: 200, date: new DateOnly(2026, 1, 10)));

        var (latest, sinceFill, fuelMileage, _) = await _harness.Mileage.GetLatestRecordAsync("Civic");

        Assert.NotNull(latest);
        Assert.Equal(200, sinceFill);
        Assert.Equal(4.00m, fuelMileage);
    }

    [Fact]
    public async Task GetLatestRecordAsync_Imperial_ConvertsToMilesAndMpg()
    {
        await _harness.Settings.SaveSettingsAsync(AppTestHarness.Setting("country", "United States"));
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(
            odoStart: 1000, odoEnd: 1100, distance: 400, mileage: 1, fuelFilled: 40m, gasStation: "Shell", date: new DateOnly(2026, 1, 1)));
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(
            odoStart: 1100, odoEnd: 1500, distance: 400, date: new DateOnly(2026, 1, 10)));

        var (_, sinceFill, fuelMileage, _) = await _harness.Mileage.GetLatestRecordAsync("Civic");

        Assert.Equal(249, sinceFill);
        Assert.Equal(23.56m, fuelMileage);
    }
}
