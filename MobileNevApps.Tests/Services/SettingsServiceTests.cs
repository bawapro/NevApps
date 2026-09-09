using MobileNevApps.Tests.Helpers;
using NevDBClass.Models;

namespace MobileNevApps.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly AppTestHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task SaveSettingsAsync_AddsUpdatesAndRemovesByKey()
    {
        await _harness.Settings.SaveSettingsAsync(
            AppTestHarness.Setting("vehicle", "Civic"),
            AppTestHarness.Setting("vehicle", "Tesla"));

        await _harness.Settings.SaveSettingsAsync(
            AppTestHarness.Setting("vehicle", "Civic", enabled: 0),
            AppTestHarness.Setting("vehicle", "Bolt"));

        var vehicles = (await _harness.Settings.GetSettingsAsync())
            .Where(s => s.SettingKey == "vehicle")
            .OrderBy(s => s.SettingValue)
            .ToList();

        Assert.Equal(2, vehicles.Count);
        Assert.Contains(vehicles, v => v.SettingValue == "Civic" && v.IsEnabled == 0);
        Assert.Contains(vehicles, v => v.SettingValue == "Bolt");
        Assert.DoesNotContain(vehicles, v => v.SettingValue == "Tesla");
    }

    [Fact]
    public void AllCountries_IncludesCanadaAndUnitedStates()
    {
        Assert.Contains(_harness.Settings.AllCountries, c => c.Name == "Canada" && c.Code == "CA");
        Assert.Contains(_harness.Settings.AllCountries, c => c.Name == "United States" && c.Code == "US");
        Assert.Contains(_harness.Settings.AllCountries, c => c.Name == "Canada" && c.CurrencyCode == "CAD");
    }

    [Fact]
    public void GetCultureCode_ResolvesEnglishCultures()
    {
        Assert.Equal("en-CA", _harness.Settings.GetCultureCode("Canada"));
        Assert.Equal("en-US", _harness.Settings.GetCultureCode("United States"));
        Assert.Equal("", _harness.Settings.GetCultureCode(""));
        Assert.Equal("en-US", _harness.Settings.GetCultureCode("NotACountry"));
    }

    [Fact]
    public async Task GetCurrencyIcon_DiffersByRegion()
    {
        await _harness.Settings.SaveSettingsAsync(AppTestHarness.Setting("country", "United States"));
        var usd = await _harness.Settings.GetCurrencyIcon();

        await _harness.Settings.SaveSettingsAsync(AppTestHarness.Setting("country", "United Kingdom"));
        var gbp = await _harness.Settings.GetCurrencyIcon();

        await _harness.Settings.SaveSettingsAsync(AppTestHarness.Setting("country", "France"));
        var eur = await _harness.Settings.GetCurrencyIcon();

        await _harness.Settings.SaveSettingsAsync(AppTestHarness.Setting("country", "India"));
        var inr = await _harness.Settings.GetCurrencyIcon();

        Assert.False(string.IsNullOrWhiteSpace(usd));
        Assert.NotEqual(usd, gbp);
        Assert.NotEqual(gbp, eur);
        Assert.NotEqual(eur, inr);
    }

    [Fact]
    public async Task GetFeaturesAsync_ReadsEnabledFlags()
    {
        await _harness.Settings.SaveSettingsAsync(
            AppTestHarness.Setting("expensetracker", "1", 1),
            AppTestHarness.Setting("mileagetracker", "1", 0),
            AppTestHarness.Setting("shenshaicalendar", "1", 1),
            AppTestHarness.Setting("stepstracker", "1", 0));

        var (expense, mileage, shenshai, steps) = await _harness.Settings.GetFeaturesAsync();
        Assert.True(expense);
        Assert.False(mileage);
        Assert.True(shenshai);
        Assert.False(steps);
    }

    [Fact]
    public async Task GetFeaturesAsync_WhenMissing_ReturnsAllFalse()
    {
        var features = await _harness.Settings.GetFeaturesAsync();
        Assert.Equal((false, false, false, false), features);
    }

    [Fact]
    public async Task VehicleImagePath_RoundTrips()
    {
        await _harness.Settings.UpdateVehicleImagePathAsync("Civic", "/images/civic.png");
        Assert.Equal("/images/civic.png", await _harness.Settings.GetVehicleImageAsync("Civic"));

        await _harness.Settings.UpdateVehicleImagePathAsync("Civic", "/images/civic2.png");
        Assert.Equal("/images/civic2.png", await _harness.Settings.GetVehicleImageAsync("Civic"));
        Assert.Equal("", await _harness.Settings.GetVehicleImageAsync("Missing"));
    }

    [Fact]
    public async Task CheckDashboardCountsAsync_ReflectsTrackerData()
    {
        await _harness.Settings.SaveSettingsAsync(
            AppTestHarness.Setting("expensetracker", "1"),
            AppTestHarness.Setting("shenshaicalendar", "1"));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(autoPay: 2));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(
            place: "Rent", autoPay: 1, autoPayDate: DateOnly.FromDateTime(DateTime.Today),
            frequency: "M", price: 1, detail: "Auto"));
        await _harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry());

        var (expense, autoPay, mileage, shenshai) = await _harness.Settings.CheckDashboardCountsAsync();
        Assert.True(expense);
        Assert.True(autoPay);
        Assert.True(mileage);
        Assert.True(shenshai);
        Assert.True(await _harness.Settings.CheckDashboardAsync());
    }
}
