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
        await _harness.Settings.UpdateVehicleImagePathAsync("Civic", "data:image/jpeg;base64,AAAA");
        Assert.Equal("data:image/jpeg;base64,AAAA", await _harness.Settings.GetVehicleImageAsync("Civic"));

        await _harness.Settings.UpdateVehicleImagePathAsync("Civic", "data:image/jpeg;base64,BBBB");
        Assert.Equal("data:image/jpeg;base64,BBBB", await _harness.Settings.GetVehicleImageAsync("Civic"));
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

    // ---- Multi-currency ----

    [Fact]
    public void FormatAmount_HomeCurrency_UsesCultureCurrencyFormat()
    {
        var text = _harness.Settings.FormatAmount(1234.5m, "CAD", "CAD", "en-US");
        Assert.Equal(1234.5m.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")), text);
        Assert.Contains("1,234.50", text);
    }

    [Fact]
    public void FormatAmount_NullCurrency_TreatedAsHomeCurrency()
    {
        var text = _harness.Settings.FormatAmount(5m, null, "cad", "");
        Assert.Equal(5m.ToString("C", System.Globalization.CultureInfo.InvariantCulture), text);
    }

    [Fact]
    public void FormatAmount_ForeignCurrency_ShowsInvariantNumberAndCode()
    {
        var text = _harness.Settings.FormatAmount(1234.5m, " usd ", "CAD", "fr-FR");
        Assert.Equal("1,234.50 (USD)", text);
    }

    [Fact]
    public async Task GetHomeCurrencyCodeAsync_DefaultsToCad()
    {
        Assert.Equal("CAD", await _harness.Settings.GetHomeCurrencyCodeAsync());
    }

    [Fact]
    public async Task GetHomeCurrencyCodeAsync_NormalizesStoredValue()
    {
        await _harness.Settings.SaveSettingsAsync(AppTestHarness.Setting("currency", "usd (United States)"));
        Assert.Equal("USD", await _harness.Settings.GetHomeCurrencyCodeAsync());
    }

    [Fact]
    public async Task SaveExtraCurrencyCodesAsync_RoundTrip_NormalizesFiltersAndDedupes()
    {
        await _harness.Settings.SaveSettingsAsync(AppTestHarness.Setting("currency", "CAD"));
        await _harness.Settings.SaveExtraCurrencyCodesAsync(new[] { "usd", "EUR", "cad", "XYZ", "USD", " " });

        var extras = await _harness.Settings.GetExtraCurrencyCodesAsync();
        Assert.Equal(new[] { "EUR", "USD" }, extras.OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task SaveExtraCurrencyCodesAsync_ReplacesPreviousList()
    {
        await _harness.Settings.SaveExtraCurrencyCodesAsync(new[] { "USD", "EUR" });
        await _harness.Settings.SaveExtraCurrencyCodesAsync(new[] { "GBP" });

        Assert.Equal(new[] { "GBP" }, (await _harness.Settings.GetExtraCurrencyCodesAsync()).ToArray());

        await _harness.Settings.SaveExtraCurrencyCodesAsync(Array.Empty<string>());
        Assert.Empty(await _harness.Settings.GetExtraCurrencyCodesAsync());
    }

    [Fact]
    public async Task GetExpenseCurrencyCodesAsync_HomeFirstThenExtras()
    {
        await _harness.Settings.SaveSettingsAsync(AppTestHarness.Setting("currency", "CAD"));
        await _harness.Settings.SaveExtraCurrencyCodesAsync(new[] { "USD", "JPY" });

        var codes = await _harness.Settings.GetExpenseCurrencyCodesAsync();
        Assert.Equal("CAD", codes[0]);
        Assert.Equal(new[] { "CAD", "JPY", "USD" }, codes.OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task GetExpenseCurrencyCodesAsync_UnsupportedHomeCurrency_IsLeftOut()
    {
        await _harness.Settings.SaveSettingsAsync(AppTestHarness.Setting("currency", "BRL"));
        await _harness.Settings.SaveExtraCurrencyCodesAsync(new[] { "USD" });

        Assert.Equal(new[] { "USD" }, (await _harness.Settings.GetExpenseCurrencyCodesAsync()).ToArray());
    }

    [Fact]
    public void GetCurrencyDisplay_UsesCodeAndCountry()
    {
        Assert.Equal("USD - United States", _harness.Settings.GetCurrencyDisplay("usd"));
        Assert.Equal("EUR - European Union", _harness.Settings.GetCurrencyDisplay("EUR"));
        Assert.Equal("", _harness.Settings.GetCurrencyDisplay(null));
    }

    [Fact]
    public void GetCurrencyOption_ReturnsNormalizedCodeAndCountry()
    {
        var option = _harness.Settings.GetCurrencyOption(" cad ");
        Assert.Equal("CAD", option.Code);
        Assert.Equal("Canada", option.Country);
    }

    [Theory]
    [InlineData("CAD", "canada", true)]
    [InlineData("CAD", "ca", true)]
    [InlineData("CAD", "", true)]
    [InlineData("EUR", "union", true)]
    [InlineData("CAD", "Japan", false)]
    [InlineData(null, "CAD", false)]
    public void CurrencyMatchesQuery_MatchesCodeOrCountry(string? code, string query, bool expected)
    {
        Assert.Equal(expected, _harness.Settings.CurrencyMatchesQuery(code, query));
    }

    // ---- Vehicle images ----

    private const string OnePixelPng =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    [Fact(Skip = "PlatformImage.FromStream throws PlatformNotSupportedException on plain net10.0 (no MAUI platform); run on a device/platform target.")]
    public async Task GetVehicleImageAsync_FilePath_ConvertsToJpegDataUrlAndDeletesFile()
    {
        var path = Path.Combine(_harness.TempDir, "civic.png");
        await File.WriteAllBytesAsync(path, Convert.FromBase64String(OnePixelPng));
        await _harness.Settings.UpdateVehicleImagePathAsync("Civic", path);

        var url = await _harness.Settings.GetVehicleImageAsync("Civic");

        Assert.StartsWith("data:image/jpeg;base64,", url);
        Assert.False(File.Exists(path));
        var stored = (await _harness.Settings.GetSettingsAsync()).Single(s => s.SettingKey == "Civic");
        Assert.Equal(url, stored.SettingValue);
    }

    [Fact]
    public async Task GetVehicleImageAsync_MissingFile_ReturnsEmptyAndClearsValue()
    {
        await _harness.Settings.SaveSettingsAsync(
            AppTestHarness.Setting("Civic", Path.Combine(_harness.TempDir, "nope.png")));

        var url = await _harness.Settings.GetVehicleImageAsync("Civic");

        Assert.Equal("", url);
        var stored = (await _harness.Settings.GetSettingsAsync()).Single(s => s.SettingKey == "Civic");
        Assert.Equal("", stored.SettingValue);
    }

    [Fact]
    public async Task GetVehicleImageAsync_UnknownVehicle_ReturnsEmpty()
    {
        Assert.Equal("", await _harness.Settings.GetVehicleImageAsync("Nothing"));
    }

    [Fact]
    public async Task GetVehicleImagesAsync_ReturnsStoredJpegAndEmptyForOthers()
    {
        const string jpeg = "data:image/jpeg;base64,AAAA";
        await _harness.Settings.SaveSettingsAsync(AppTestHarness.Setting("Civic", jpeg));
        await _harness.Settings.SaveSettingsAsync(
            AppTestHarness.Setting("Tesla", Path.Combine(_harness.TempDir, "missing.png")));

        var images = await _harness.Settings.GetVehicleImagesAsync(new[] { "Civic", "Tesla", "Bolt", "", "Civic" });

        Assert.Equal(3, images.Count);
        Assert.Equal(jpeg, images["Civic"]);
        Assert.Equal("", images["Tesla"]);
        Assert.Equal("", images["Bolt"]);
    }
}
