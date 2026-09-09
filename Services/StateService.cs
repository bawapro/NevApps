namespace NevApps.Services
{
    public class StateService
    {
        private readonly IPreferences _prefs;

        public StateService() : this(Preferences.Default)
        {
        }

        public StateService(IPreferences preferences)
        {
            _prefs = preferences;
        }

        #region TRIP STATE

        private const string TripFlagKey = "TripFlag";
        private const string TripNameKey = "TripName";

        public void SetTripState(bool tripFlagValue, string tripNameValue)
        {
            _prefs.Set(TripFlagKey, tripFlagValue);
            _prefs.Set(TripNameKey, tripNameValue);
        }

        public (bool IsTripActive, string TripName) GetTripState()
        {
            bool isTripActive = _prefs.Get(TripFlagKey, false); // default false
            string tripName = _prefs.Get(TripNameKey, string.Empty);

            return (isTripActive, tripName);
        }

        public void ClearTripState()
        {
            _prefs.Remove(TripFlagKey);
            _prefs.Remove(TripNameKey);
        }

        #endregion

        #region EV VEHICLE STATE

        private const string EVPrefix = "EV_";

        public void SetVehicleIsEV(string vehicleName, bool isEV)
        {
            if (string.IsNullOrWhiteSpace(vehicleName)) return;
            string key = EVPrefix + vehicleName.Trim().ToLowerInvariant();
            _prefs.Set(key, isEV);
        }

        public bool GetVehicleIsEV(string vehicleName)
        {
            if (string.IsNullOrWhiteSpace(vehicleName)) return false;
            string key = EVPrefix + vehicleName.Trim().ToLowerInvariant();
            return _prefs.Get(key, false);
        }

        public void ClearVehicleEVFlag(string vehicleName)
        {
            if (string.IsNullOrWhiteSpace(vehicleName)) return;
            string key = EVPrefix + vehicleName.Trim().ToLowerInvariant();
            _prefs.Remove(key);
        }
        

        #endregion

        #region SUNRISE SUNSET STATE

        private const string SunriseKey = "Sun_Sunrise";
        private const string SunsetKey = "Sun_Sunset";
        private const string CityKey = "Sun_City";
        private const string LastUpdateDateKey = "Sun_LastUpdate";

        public void SetSunData(string sunrise, string sunset, string city)
        {
            _prefs.Set(SunriseKey, sunrise);
            _prefs.Set(SunsetKey, sunset);
            _prefs.Set(CityKey, city);
            _prefs.Set(LastUpdateDateKey, DateTime.Today.ToString("yyyy-MM-dd"));
        }

        public (string Sunrise, string Sunset, string City, bool NeedsRefresh) GetSunData()
        {
            string sunrise = _prefs.Get(SunriseKey, string.Empty);
            string sunset = _prefs.Get(SunsetKey, string.Empty);
            string city = _prefs.Get(CityKey, "Unknown");
            string lastDateStr = _prefs.Get(LastUpdateDateKey, string.Empty);

            // If the stored date is NOT today, we need a new API call
            bool needsRefresh = lastDateStr != DateTime.Today.ToString("yyyy-MM-dd");

            return (sunrise, sunset, city, needsRefresh);
        }

        #endregion

        #region NAVROZ GREETING

        private const string NavrozYearKey = "NavrozYear";

        public bool IsNavroz(int currentYear, int currentMonth, int currentDay)
        {
            int lastYear = _prefs.Get(NavrozYearKey, 0);

            if (currentYear > lastYear && currentMonth == 0 && currentDay >= 0)
            {
                _prefs.Set(NavrozYearKey, currentYear);
                return true;
            }

            return false;
        }

        #endregion
    }
}