using Microsoft.EntityFrameworkCore;
using MudBlazor;
using NevDBClass.Data;
using NevDBClass.Models;
using System.Globalization;
using static NevApps.Models.Settings;

namespace NevApps.Services
{
    /// <summary>
    /// Manages application-wide configurations, regional localization, and user preferences.
    /// Handles the translation of database settings into UI-ready properties like currency and units.
    /// </summary>
    internal class SettingsService
    {
        private readonly IDbContextFactory<SqLiteDbContext> _db;

        /// <summary>
        /// A comprehensive list of world countries derived from system cultures.
        /// Used to populate region-specific data like currency symbols and distance units.
        /// </summary>
        public readonly List<Country> AllCountries = CultureInfo
            .GetCultures(CultureTypes.SpecificCultures)
            .Select(c => new RegionInfo(c.Name))
            .DistinctBy(r => r.TwoLetterISORegionName)
            .Select(r => new Country
            {
                Code = r.TwoLetterISORegionName,
                Name = r.EnglishName,
                CurrencyCode = r.ISOCurrencySymbol,
                CurrencySymbol = r.CurrencySymbol,
                Continent = GetContinentFromCode(r.TwoLetterISORegionName)
            })
            .OrderBy(c => c.Name)
            .ToList();

        public Country? Country { get; set; } = new();
        
        public SettingsService(IDbContextFactory<SqLiteDbContext> dbFactory)
        {
            _db = dbFactory;
        }

        /// <summary>
        /// Retrieves the full list of application settings from the local database.
        /// </summary>
        public async Task<List<Setting>> GetSettingsAsync()
        {
            using var db = await _db.CreateDbContextAsync();
            return await db.Settings.ToListAsync();
        }

        /// <summary>
        /// Syncs settings by key: deletes rows whose values are no longer in the incoming list, then inserts or updates the rest.
        /// </summary>
        public async Task<int> SaveSettingsAsync(params Setting[] newSettings)
        {
            using var db = await _db.CreateDbContextAsync();

            // 1. Group incoming settings by Key (e.g., "vehicle", "category")
            var groupedNewSettings = newSettings.GroupBy(x => x.SettingKey);

            foreach (var group in groupedNewSettings)
            {
                string currentKey = group.Key;
                var incomingItems = group.ToList();

                // 2. Get what is currently in the DB for this specific Key
                var dbItems = await db.Settings.Where(x => x.SettingKey == currentKey).ToListAsync();

                // --- SECTION A: REMOVE (Sync Deletions) ---
                // If it's in the DB but NOT in the incoming list, delete it.
                var toRemove = dbItems.Where(dbItem =>
                    !incomingItems.Any(n => n.SettingValue == dbItem.SettingValue)).ToList();

                if (toRemove.Any())
                    db.Settings.RemoveRange(toRemove);

                // --- SECTION B: UPSERT (Add or Update) ---
                foreach (var incoming in incomingItems)
                {
                    var existing = dbItems.FirstOrDefault(x => x.SettingValue == incoming.SettingValue);

                    if (existing != null)
                    {
                        // Update existing record
                        existing.IsEnabled = incoming.IsEnabled;
                        db.Settings.Update(existing);
                    }
                    else
                    {
                        // Add new record
                        db.Settings.Add(incoming);
                    }
                }
            }

            // 3. One single SaveChanges at the very end for performance and atomicity
            return await db.SaveChangesAsync();
        }

        /// <summary>
        /// Maps the user's selected country to a specific MudBlazor Material Icon.
        /// <br/><b>Regional Rule:</b> Defaults to EuroSymbol for Europe (except CH/GB) and AttachMoney for North America/Oceania.
        /// </summary>
        public async Task<string> GetCurrencyIcon()
        {
            List<Setting> Settings = await GetSettingsAsync();

            string? country = Settings.Where(x => x.SettingKey == "country").FirstOrDefault()?.SettingValue;

            if (country != null)
            {
                Country = AllCountries.FirstOrDefault(c => c.Name == country)!;
                if (Country != null)
                {
                    // Europe rule: almost all use Euro (except Switzerland, UK, etc.)
                    if (Country.Continent == "Europe")
                    {
                        // Special case: Switzerland uses Swiss Franc
                        if (Country.Code == "CH")
                            return Icons.Material.Filled.CurrencyFranc; // CHF

                        if (Country.Code == "GB")
                            return Icons.Material.Filled.CurrencyPound; // GBP


                        // All other European countries → Euro
                        return Icons.Material.Filled.EuroSymbol; // €
                    }


                    // Non-European or other special cases
                    return Country.CurrencyCode switch
                    {
                        "USD" or "CAD" or "AUD" or "NZD" or "HKD" or "SGD"
                            => Icons.Material.Filled.AttachMoney, // $

                        "GBP" => Icons.Material.Filled.CurrencyPound, // £
                        "JPY" or "CNY" => Icons.Material.Filled.CurrencyYen, // ¥ (yen and yuan share symbol)

                        "RUB" => Icons.Material.Filled.CurrencyRuble, // ₽
                        "INR" => Icons.Material.Filled.CurrencyRupee, // ₹
                        // "KRW" => Icons.Material.Filled.CurrencyWon,       // ₩

                        _ => Icons.Material.Filled.Money // fallback generic money icon
                    };
                }
            }

            return Icons.Material.Filled.AttachMoney;
        }

        /// <summary>
        /// Categorizes a two-letter ISO country code into its respective continent.
        /// </summary>
        private static string GetContinentFromCode(string code)
        {
            return code switch
            {
                // Europe (most common in your example)
                "AL" or "AD" or "AT" or "BY" or "BE" or "BA" or "BG" or "HR" or "CY" or "CZ" or "DK" or "EE" or "FO"
                    or "FI" or "FR" or "DE" or "GI" or "GR" or "HU" or "IS" or "IE" or "IT" or "XK" or "LV" or "LI"
                    or "LT" or "LU" or "MT" or "MD" or "MC" or "ME" or "NL" or "MK" or "NO" or "PL" or "PT" or "RO"
                    or "RU" or "SM" or "RS" or "SK" or "SI" or "ES" or "SE" or "CH" or "UA" or "GB" or "VA"
                    => "Europe",

                // North America
                "CA" or "US" or "MX" or "GL" => "North America",

                // Asia
                "AF" or "AM" or "AZ" or "BH" or "BD" or "BT" or "BN" or "KH" or "CN" or "GE" or "HK" or "IN" or "ID"
                    or "IR" or "IQ" or "IL" or "JP" or "JO" or "KZ" or "KW" or "KG" or "LA" or "LB" or "MO" or "MY"
                    or "MV" or "MN" or "MM" or "NP" or "KP" or "OM" or "PK" or "PS" or "PH" or "QA" or "SA" or "SG"
                    or "KR" or "LK" or "SY" or "TW" or "TJ" or "TH" or "TL" or "TR" or "TM" or "AE" or "UZ" or "VN"
                    or "YE"
                    => "Asia",

                // Africa (example subset)
                "DZ" or "AO" or "BJ" or "BW" or "BF" or "BI" or "CV" or "CM" or "CF" or "TD" or "KM" or "CD" or "CG"
                    or "CI" or "DJ" or "EG" or "GQ" or "ER" or "SZ" or "ET" or "GA" or "GM" or "GH" or "GN" or "GW"
                    or "KE" or "LS" or "LR" or "LY" or "MG" or "MW" or "ML" or "MR" or "MU" or "MA" or "MZ" or "NA"
                    or "NE" or "NG" or "RW" or "ST" or "SN" or "SC" or "SL" or "SO" or "ZA" or "SS" or "SD" or "TZ"
                    or "TG" or "TN" or "UG" or "EH" or "ZM" or "ZW"
                    => "Africa",

                // Add other continents as needed: South America, Oceania, Antarctica (rare)
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Resolves the .NET CultureInfo name for a given country.
        /// <br/><b>Logic:</b> Attempts to find an English-language culture (en-XX) matching the country name.
        /// </summary>
        public string GetCultureCode(string countryName)
        {
            if (string.IsNullOrWhiteSpace(countryName))
                return CultureInfo.InvariantCulture.Name;

            var country = AllCountries
                .FirstOrDefault(c => string.Equals(c.Name, countryName, StringComparison.OrdinalIgnoreCase));

            if (country == null)
                return "en-US";

            var culture = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .FirstOrDefault(c =>
                {
                    try
                    {
                        var ri = new RegionInfo(c.Name);

                        // Prefer en-* (e.g. en-CA) over other languages for the same country.
                        return ri.EnglishName.Equals(countryName, StringComparison.OrdinalIgnoreCase) &&
                               c.TwoLetterISOLanguageName == "en";
                    }
                    catch
                    {
                        return false;
                    }
                });

            return culture?.Name ?? $"en-{country.Code}";
        }

        /// <summary>
        /// Checks which major features (Expense, Mileage, Shenshai, Steps) are currently enabled in the app.
        /// </summary>
        public async Task<(bool isExpenseEnabled, bool isMileageEnabled, bool isShenshaiEnabled, bool isStepsEnabled)>
            GetFeaturesAsync()
        {
            using var db = await _db.CreateDbContextAsync();

            var featureKeys = new[] { "expensetracker", "mileagetracker", "shenshaicalendar", "stepstracker" };
            var features = await db.Settings.AsNoTracking()
                .Where(x => featureKeys.Contains(x.SettingKey))
                .ToListAsync();

            if (features.Count == 0)
                return (false, false, false, false);

            bool isExpenseEnabled = Convert.ToBoolean(features
                .FirstOrDefault(x => x.SettingKey == "expensetracker")?.IsEnabled ?? 0);
            bool isMileageEnabled = Convert.ToBoolean(features
                .FirstOrDefault(x => x.SettingKey == "mileagetracker")?.IsEnabled ?? 0);
            bool isShenshaiEnabled = Convert.ToBoolean(features
                .FirstOrDefault(x => x.SettingKey == "shenshaicalendar")?.IsEnabled ?? 0);
            bool isStepsEnabled = Convert.ToBoolean(features
                .FirstOrDefault(x => x.SettingKey == "stepstracker")?.IsEnabled ?? 0);
            return (isExpenseEnabled, isMileageEnabled, isShenshaiEnabled, isStepsEnabled);
        }

        /// <summary>
        /// Retrieves the local file path for a vehicle's custom image.
        /// </summary>
        public async Task<string> GetVehicleImageAsync(string vehicle)
        {
            using var db = await _db.CreateDbContextAsync();
            var setting = await db.Settings.FirstOrDefaultAsync(v => v.SettingKey == vehicle);
            return setting?.SettingValue ?? "";
        }

        /// <summary>
        /// Updates or creates the file path association for a vehicle's display image.
        /// </summary>
        public async Task UpdateVehicleImagePathAsync(string vehicleName, string path)
        {
            using var db = await _db.CreateDbContextAsync();
            var existing = await db.Settings.FirstOrDefaultAsync(v => v.SettingKey == vehicleName);
            if (existing != null)
            {
                existing.SettingValue = path;
                db.Settings.Update(existing);
            }
            else
            {
                var setting = new Setting
                {
                    SettingKey = vehicleName,
                    SettingValue = path,
                    IsEnabled = 1
                };
                db.Settings.Add(setting);
            }

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Scans trackers to determine if data exists for various dashboard widgets.
        /// </summary>
        public async Task<(bool expenseCount, bool isAutoPay, bool isMileage, bool isShenshai)> CheckDashboardCountsAsync()
        {
            using var db = await _db.CreateDbContextAsync();
            bool exp = await db.ExpenseTrackers.AnyAsync(x => x.AutoPay != 1 && x.AutoPay != 0);
            bool pay = await db.ExpenseTrackers.AnyAsync(x => x.AutoPay == 1);
            bool mil = await db.MileageTrackers.AnyAsync();
            bool she = await db.Settings.AnyAsync(x => x.SettingKey == "shenshaicalendar" && x.IsEnabled == 1);

            return (exp, pay, mil, she);
        }

        /// <summary>
        /// Quick check to see if any main tracking features are initialized.
        /// </summary>
        public async Task<bool> CheckDashboardAsync()
        {
            using var db = await _db.CreateDbContextAsync();
            return await db.Settings.AnyAsync(x =>
                (x.SettingKey == "expensetracker" ||
                 x.SettingKey == "mileagetracker" ||
                 x.SettingKey == "shenshaicalendar") &&
                x.IsEnabled == 1);
        }
    }
}