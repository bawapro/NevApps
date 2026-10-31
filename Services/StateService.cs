using NevApps.Classes;

namespace NevApps.Services;

public class StateService
{
    internal const string ThemePreferenceKey = "AppTheme";
    private readonly IPreferences _preferences;

    public StateService() : this(Preferences.Default)
    {
    }

    public StateService(IPreferences preferences)
    {
        _preferences = preferences;
    }


    #region DASHBOARD

    /// <summary>
    ///     Set when an expense or mileage record is added, edited, or deleted.
    ///     Dashboard consumes this on the next visit and reloads.
    /// </summary>
    public bool DashboardReload { get; set; }

    #endregion

    #region TRIP STATE

    private const string TripFlagKey = "TripFlag";
    private const string TripNameKey = "TripName";

    public void SetTripState(bool tripFlagValue, string tripNameValue)
    {
        _preferences.Set(TripFlagKey, tripFlagValue);
        _preferences.Set(TripNameKey, tripNameValue);
    }

    public (bool IsTripActive, string TripName) GetTripState()
    {
        var isTripActive = _preferences.Get(TripFlagKey, false); // default false
        var tripName = _preferences.Get(TripNameKey, string.Empty);

        return (isTripActive, tripName);
    }

    public void ClearTripState()
    {
        _preferences.Remove(TripFlagKey);
        _preferences.Remove(TripNameKey);
    }

    #endregion

    #region EV VEHICLE STATE

    private const string EVPrefix = "EV_";

    public void SetVehicleIsEV(string vehicleName, bool isEV)
    {
        if (string.IsNullOrWhiteSpace(vehicleName)) return;
        var key = EVPrefix + vehicleName.Trim().ToLowerInvariant();
        _preferences.Set(key, isEV);
    }

    public bool GetVehicleIsEV(string vehicleName)
    {
        if (string.IsNullOrWhiteSpace(vehicleName)) return false;
        var key = EVPrefix + vehicleName.Trim().ToLowerInvariant();
        return _preferences.Get(key, false);
    }

    public void ClearVehicleEVFlag(string vehicleName)
    {
        if (string.IsNullOrWhiteSpace(vehicleName)) return;
        var key = EVPrefix + vehicleName.Trim().ToLowerInvariant();
        _preferences.Remove(key);
    }

    #endregion

    #region NEVAPI STATE

    private const bool IsNevAPIOn = true;

    public void SetNevAPIState(bool isOn)
    {
        _preferences.Set(nameof(IsNevAPIOn), isOn);
    }

    public bool GetNevAPIState()
    {
        return _preferences.Get(nameof(IsNevAPIOn), IsNevAPIOn);
    }

    #endregion

    #region SUNRISE SUNSET STATE

    private const string SunriseKey = "Sun_Sunrise";
    private const string SunsetKey = "Sun_Sunset";
    private const string CityKey = "Sun_City";
    private const string LastUpdateDateKey = "Sun_LastUpdate";

    public void SetSunData(string sunrise, string sunset, string city)
    {
        _preferences.Set(SunriseKey, sunrise);
        _preferences.Set(SunsetKey, sunset);
        _preferences.Set(CityKey, city);
        _preferences.Set(LastUpdateDateKey, DateTime.Today.ToString("yyyy-MM-dd"));
    }

    public (string Sunrise, string Sunset, string City, bool NeedsRefresh) GetSunData()
    {
        var sunrise = _preferences.Get(SunriseKey, string.Empty);
        var sunset = _preferences.Get(SunsetKey, string.Empty);
        var city = _preferences.Get(CityKey, "Unknown");
        var lastDateStr = _preferences.Get(LastUpdateDateKey, string.Empty);

        // If the stored date is NOT today, we need a new API call
        var needsRefresh = lastDateStr != DateTime.Today.ToString("yyyy-MM-dd");

        return (sunrise, sunset, city, needsRefresh);
    }

    #endregion

    #region APPEARANCE

    public event Action? ThemeChanged;

    /// <summary>
    ///     Fired when a support reply is saved or marked read, so the settings badge can refresh.
    /// </summary>
    public event Action? SupportNoticesChanged;

    public void NotifySupportNoticesChanged() => SupportNoticesChanged?.Invoke();

    private bool _adminSupportHasEntries;
    public bool HasAdminSupportEntries => _adminSupportHasEntries;
    public event Action? AdminSupportChanged;

    public void SetAdminSupportHasEntries(bool hasEntries)
    {
        if (_adminSupportHasEntries == hasEntries)
            return;

        _adminSupportHasEntries = hasEntries;
        AdminSupportChanged?.Invoke();
    }

    public AppThemeMode GetTheme()
    {
        var value = _preferences.Get(ThemePreferenceKey, nameof(AppThemeMode.System));
        return Enum.TryParse<AppThemeMode>(value, out var mode) ? mode : AppThemeMode.System;
    }

    public bool IsOsDark()
    {
        try
        {
            return Application.Current?.RequestedTheme == AppTheme.Dark;
        }
        catch
        {
            return false;
        }
    }

    public bool IsDark()
    {
        return GetTheme() switch
        {
            AppThemeMode.Dark => true,
            AppThemeMode.Light => false,
            _ => IsOsDark()
        };
    }

    public void SetTheme(AppThemeMode mode)
    {
        _preferences.Set(ThemePreferenceKey, mode.ToString());
        ApplyToApplication();
        ThemeChanged?.Invoke();
    }

    public void ApplyToApplication()
    {
        try
        {
            var app = Application.Current;
            if (app is null)
                return;

            app.UserAppTheme = GetTheme() switch
            {
                AppThemeMode.Light => AppTheme.Light,
                AppThemeMode.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
        catch
        {
            // No MAUI window in unit tests
        }
    }

    #endregion

    #region NAVROZ GREETING

    private const string NavrozYearKey = "NavrozYear";

    public bool IsNavroz(int currentYear, int currentMonth, int currentDay)
    {
        var lastYear = _preferences.Get(NavrozYearKey, 0);

        if (currentYear > lastYear && currentMonth == 0 && currentDay >= 0)
        {
            _preferences.Set(NavrozYearKey, currentYear);
            return true;
        }

        return false;
    }

    #endregion
}