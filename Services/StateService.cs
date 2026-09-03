namespace NevApps.Services
{
    public class StateService
    {
        #region TRIP STATE

        private const string TripFlagKey = "TripFlag";
        private const string TripNameKey = "TripName";

        public void SetTripState(bool tripFlagValue, string tripNameValue)
        {
            Preferences.Default.Set(TripFlagKey, tripFlagValue);
            Preferences.Default.Set(TripNameKey, tripNameValue);
        }

        public (bool IsTripActive, string TripName) GetTripState()
        {
            bool isTripActive = Preferences.Default.Get(TripFlagKey, false); // default false
            string tripName = Preferences.Default.Get(TripNameKey, string.Empty);

            return (isTripActive, tripName);
        }

        public void ClearTripState()
        {
            Preferences.Default.Remove(TripFlagKey);
            Preferences.Default.Remove(TripNameKey);
        }

        #endregion

        #region EV VEHICLE STATE

        private const string EVPrefix = "EV_";

        public void SetVehicleIsEV(string vehicleName, bool isEV)
        {
            if (string.IsNullOrWhiteSpace(vehicleName)) return;
            string key = EVPrefix + vehicleName.Trim().ToLowerInvariant();
            Preferences.Default.Set(key, isEV);
        }

        public bool GetVehicleIsEV(string vehicleName)
        {
            if (string.IsNullOrWhiteSpace(vehicleName)) return false;
            string key = EVPrefix + vehicleName.Trim().ToLowerInvariant();
            return Preferences.Default.Get(key, false);
        }

        public void ClearVehicleEVFlag(string vehicleName)
        {
            if (string.IsNullOrWhiteSpace(vehicleName)) return;
            string key = EVPrefix + vehicleName.Trim().ToLowerInvariant();
            Preferences.Default.Remove(key);
        }
        

        #endregion

        #region SUNRISE SUNSET STATE

        private const string SunriseKey = "Sun_Sunrise";
        private const string SunsetKey = "Sun_Sunset";
        private const string CityKey = "Sun_City";
        private const string LastUpdateDateKey = "Sun_LastUpdate";

        public void SetSunData(string sunrise, string sunset, string city)
        {
            Preferences.Default.Set(SunriseKey, sunrise);
            Preferences.Default.Set(SunsetKey, sunset);
            Preferences.Default.Set(CityKey, city);
            Preferences.Default.Set(LastUpdateDateKey, DateTime.Today.ToString("yyyy-MM-dd"));
        }

        public (string Sunrise, string Sunset, string City, bool NeedsRefresh) GetSunData()
        {
            string sunrise = Preferences.Default.Get(SunriseKey, string.Empty);
            string sunset = Preferences.Default.Get(SunsetKey, string.Empty);
            string city = Preferences.Default.Get(CityKey, "Unknown");
            string lastDateStr = Preferences.Default.Get(LastUpdateDateKey, string.Empty);

            // If the stored date is NOT today, we need a new API call
            bool needsRefresh = lastDateStr != DateTime.Today.ToString("yyyy-MM-dd");

            return (sunrise, sunset, city, needsRefresh);
        }

        #endregion

        #region NAVROZ GREETING

        private const string NavrozYearKey = "NavrozYear";

        public bool IsNavroz(int currentYear, int currentMonth, int currentDay)
        {
            int lastYear = Preferences.Default.Get(NavrozYearKey, 0);

            if (currentYear > lastYear && currentMonth == 0 && currentDay >= 0)
            {
                Preferences.Default.Set(NavrozYearKey, currentYear);
                return true;
            }

            return false;
        }

        #endregion
    }
}