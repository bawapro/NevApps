using NevApps.Services;
using MobileNevApps.Tests.Helpers;

namespace MobileNevApps.Tests.Services;

public class StateServiceTests
{
    private readonly InMemoryPreferences _prefs = new();
    private readonly StateService _state;

    public StateServiceTests()
    {
        _state = new StateService(_prefs);
    }

    [Fact]
    public void TripState_SetGetAndClear()
    {
        _state.SetTripState(true, "Montreal");
        var (active, name) = _state.GetTripState();

        Assert.True(active);
        Assert.Equal("Montreal", name);

        _state.ClearTripState();
        var cleared = _state.GetTripState();
        Assert.False(cleared.IsTripActive);
        Assert.Equal(string.Empty, cleared.TripName);
    }

    [Fact]
    public void VehicleEvFlag_IsCaseInsensitiveAndClears()
    {
        _state.SetVehicleIsEV(" Tesla Model 3 ", true);
        Assert.True(_state.GetVehicleIsEV("tesla model 3"));
        Assert.False(_state.GetVehicleIsEV("Civic"));
        Assert.False(_state.GetVehicleIsEV(" "));

        _state.ClearVehicleEVFlag("TESLA MODEL 3");
        Assert.False(_state.GetVehicleIsEV("Tesla Model 3"));
    }

    [Fact]
    public void SunData_SetToday_DoesNotNeedRefresh()
    {
        _state.SetSunData("06:12", "19:44", "Toronto");
        var (sunrise, sunset, city, needsRefresh) = _state.GetSunData();

        Assert.Equal("06:12", sunrise);
        Assert.Equal("19:44", sunset);
        Assert.Equal("Toronto", city);
        Assert.False(needsRefresh);
    }

    [Fact]
    public void SunData_StaleDate_NeedsRefresh()
    {
        _prefs.Set("Sun_Sunrise", "06:00");
        _prefs.Set("Sun_Sunset", "18:00");
        _prefs.Set("Sun_City", "Mumbai");
        _prefs.Set("Sun_LastUpdate", DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"));

        var data = _state.GetSunData();
        Assert.Equal("Mumbai", data.City);
        Assert.True(data.NeedsRefresh);
    }

    [Fact]
    public void IsNavroz_OnlyFiresOncePerYearOnMonthZero()
    {
        Assert.True(_state.IsNavroz(2026, 0, 1));
        Assert.False(_state.IsNavroz(2026, 0, 2));
        Assert.False(_state.IsNavroz(2026, 1, 1));
        Assert.True(_state.IsNavroz(2027, 0, 0));
    }
}
