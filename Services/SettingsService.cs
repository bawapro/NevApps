using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using MudBlazor;
using NevDBClass.Data;
using NevDBClass.Models;
using static NevApps.Models.Settings;

namespace NevApps.Services;

/// <summary>
///     Manages application-wide configurations, regional localization, and user preferences.
///     Handles the translation of database settings into UI-ready properties like currency and units.
/// </summary>
internal class SettingsService
{
    private static readonly HashSet<string> CurrencyIconCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "AUD", "CAD", "CHF", "CNY", "EUR", "GBP", "HKD", "INR",
        "JPY", "NZD", "RUB", "SGD", "USD"
    };

    private static readonly Dictionary<string, string> PreferredCurrencyRegions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AUD"] = "AU",
        ["CAD"] = "CA",
        ["CHF"] = "CH",
        ["CNY"] = "CN",
        ["GBP"] = "GB",
        ["HKD"] = "HK",
        ["INR"] = "IN",
        ["JPY"] = "JP",
        ["NZD"] = "NZ",
        ["RUB"] = "RU",
        ["SGD"] = "SG",
        ["USD"] = "US"
    };

    private static readonly Dictionary<string, string> CurrencyCountryOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EUR"] = "European Union"
    };

    /// <summary>
    ///     A comprehensive list of world countries derived from system cultures.
    ///     Used to populate region-specific data like currency symbols and distance units.
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

    private readonly IDbContextFactory<SqLiteDbContext> _db;

    public SettingsService(IDbContextFactory<SqLiteDbContext> dbFactory)
    {
        _db = dbFactory;
    }

    public Country? Country { get; set; } = new();

    // A stored photo is a JPEG data URL. Anything else on that row is still a file path,
    // or a PNG/GIF/WebP data URL left by the first import. Both of those are converted once.
    private const string ImagePrefix = "data:image/";
    private const string JpegPrefix = "data:image/jpeg";
    private const int JpegMaxWidth = 800;
    private readonly ConcurrentDictionary<string, string> _imageCache = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ImageGates = new();

    public async Task<List<Setting>> GetSettingsAsync()
    {
        using var db = await _db.CreateDbContextAsync();
        // Photos use the vehicle name as SettingKey. Skip those keys so this read
        // never opens the image text. Names themselves live on SettingKey == "vehicle".
        var imageKeys = await db.Settings
            .Where(s => s.SettingKey == "vehicle" && s.SettingValue != "")
            .Select(s => s.SettingValue)
            .ToListAsync()
            .ConfigureAwait(false);

        IQueryable<Setting> query = db.Settings;
        if (imageKeys.Count > 0)
            query = query.Where(s => !imageKeys.Contains(s.SettingKey));
        return await query.ToListAsync().ConfigureAwait(false);
    }

    public bool TryGetCachedVehicleImage(string vehicle, out string url) =>
        _imageCache.TryGetValue(vehicle, out url!);

    public async Task<Dictionary<string, string>> GetVehicleImagesAsync(IEnumerable<string> vehicles)
    {
        var wanted = vehicles.Where(v => !string.IsNullOrEmpty(v)).Distinct().ToList();
        var result = new Dictionary<string, string>(wanted.Count);
        var missing = new List<string>();
        foreach (var vehicle in wanted)
        {
            if (_imageCache.TryGetValue(vehicle, out var cached))
                result[vehicle] = cached;
            else
                missing.Add(vehicle);
        }

        if (missing.Count == 0)
            return result;

        List<(string Key, string Value)> rows;
        using (var db = await _db.CreateDbContextAsync().ConfigureAwait(false))
        {
            var raw = await db.Settings
                .AsNoTracking()
                .Where(s => missing.Contains(s.SettingKey))
                .Select(s => new { s.SettingKey, s.SettingValue })
                .ToListAsync()
                .ConfigureAwait(false);
            rows = raw.Select(s => (s.SettingKey, s.SettingValue)).ToList();
        }

        var found = new HashSet<string>();
        foreach (var (key, raw) in rows)
        {
            found.Add(key);
            var value = raw ?? "";
            if (IsStoredJpeg(value) || value.Length == 0)
            {
                _imageCache[key] = value;
                result[key] = value;
                continue;
            }

            try
            {
                result[key] = await GetVehicleImageAsync(key).ConfigureAwait(false);
            }
            catch
            {
                // Leave the row unchanged. The next launch tries the file or the old data URL again.
                result[key] = "";
            }
        }

        foreach (var vehicle in missing)
        {
            if (found.Contains(vehicle) || result.ContainsKey(vehicle))
                continue;
            _imageCache[vehicle] = "";
            result[vehicle] = "";
        }

        return result;
    }

    public async Task<string> GetVehicleImageAsync(string vehicle)
    {
        if (_imageCache.TryGetValue(vehicle, out var cached))
            return cached;

        var gate = ImageGates.GetOrAdd(vehicle, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_imageCache.TryGetValue(vehicle, out cached))
                return cached;

            using var db = await _db.CreateDbContextAsync().ConfigureAwait(false);
            var setting = await db.Settings
                .FirstOrDefaultAsync(v => v.SettingKey == vehicle)
                .ConfigureAwait(false);
            if (setting == null)
            {
                _imageCache[vehicle] = "";
                return "";
            }

            var value = setting.SettingValue ?? "";
            if (IsStoredJpeg(value))
            {
                _imageCache[vehicle] = value;
                return value;
            }

            if (value.StartsWith(ImagePrefix, StringComparison.Ordinal))
            {
                var jpeg = await ReplaceWithJpegAsync(db, setting, DecodeDataUrl(value)).ConfigureAwait(false);
                _imageCache[vehicle] = jpeg;
                return jpeg;
            }

            if (value.Length == 0)
            {
                _imageCache[vehicle] = "";
                return "";
            }

            var path = value;
            if (!File.Exists(path))
            {
                setting.SettingValue = "";
                await db.SaveChangesAsync().ConfigureAwait(false);
                _imageCache[vehicle] = "";
                return "";
            }

            var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            var dataUrl = await ReplaceWithJpegAsync(db, setting, bytes).ConfigureAwait(false);
            try
            {
                File.Delete(path);
            }
            catch
            {
                // The row already holds the JPEG. A leftover file is not read again.
            }

            _imageCache[vehicle] = dataUrl;
            return dataUrl;
        }
        finally
        {
            gate.Release();
        }
    }

    private static bool IsStoredJpeg(string value) =>
        value.StartsWith(JpegPrefix, StringComparison.OrdinalIgnoreCase);

    // Save first. The file is removed only after the row no longer points at it.
    private async Task<string> ReplaceWithJpegAsync(SqLiteDbContext db, Setting setting, byte[] bytes)
    {
        var dataUrl = ToJpegDataUrl(bytes);
        setting.SettingValue = dataUrl;
        await db.SaveChangesAsync().ConfigureAwait(false);
        return dataUrl;
    }

    private static byte[] DecodeDataUrl(string dataUrl)
    {
        var comma = dataUrl.IndexOf(',');
        if (comma < 0 || comma >= dataUrl.Length - 1)
            throw new InvalidOperationException("The stored image is not a data URL.");
        return Convert.FromBase64String(dataUrl[(comma + 1)..]);
    }

    internal static string ToJpegDataUrl(byte[] source)
    {
        using var ms = new MemoryStream(source);
        var image = PlatformImage.FromStream(ms)
            ?? throw new InvalidOperationException("The data could not be read as an image.");

        var width = Math.Max(1f, image.Width);
        var targetWidth = Math.Max(1, (int)Math.Min(JpegMaxWidth, width));
        var targetHeight = Math.Max(1, (int)(targetWidth * (image.Height / width)));

        using var resized = image.Resize(targetWidth, targetHeight, disposeOriginal: true);
        var jpeg = resized.AsBytes(ImageFormat.Jpeg, 0.8f);
        return $"data:image/jpeg;base64,{Convert.ToBase64String(jpeg)}";
    }

    public async Task UpdateVehicleImagePathAsync(string vehicleName, string dataUrl)
    {
        dataUrl ??= "";

        using var db = await _db.CreateDbContextAsync();
        var existing = await db.Settings.FirstOrDefaultAsync(v => v.SettingKey == vehicleName);
        if (existing != null)
        {
            existing.SettingValue = dataUrl;
        }
        else
        {
            db.Settings.Add(new Setting
            {
                SettingKey = vehicleName,
                SettingValue = dataUrl,
                IsEnabled = 1
            });
        }

        await db.SaveChangesAsync();
        _imageCache[vehicleName] = dataUrl;
    }

    /// <summary>
    ///     Updates existing settings or adds new ones if the keys do not exist.
    ///     <br /><b>Business Rule:</b> Special handling for "category" keys to ensure they are updated correctly.
    /// </summary>
    public async Task<int> SaveSettingsAsync(params Setting[] newSettings)
    {
        using var db = await _db.CreateDbContextAsync();

        // 1. Group incoming settings by Key (e.g., "vehicle", "category")
        var groupedNewSettings = newSettings.GroupBy(x => x.SettingKey);

        foreach (var group in groupedNewSettings)
        {
            var currentKey = group.Key;
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
                    // You can update other fields here if needed
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
    ///     Maps the user's selected country to a specific MudBlazor Material Icon.
    ///     <br /><b>Regional Rule:</b> Defaults to EuroSymbol for Europe (except CH/GB) and AttachMoney for North
    ///     America/Oceania.
    /// </summary>
    public async Task<string> GetCurrencyIcon()
    {
        var Settings = await GetSettingsAsync();

        var country = Settings.Where(x => x.SettingKey == "country").FirstOrDefault()?.SettingValue;

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
    ///     Categorizes a two-letter ISO country code into its respective continent.
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
    ///     Resolves the .NET CultureInfo name for a given country.
    ///     <br /><b>Logic:</b> Attempts to find an English-language culture (en-XX) matching the country name.
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

                    // This returns csw-CA for Canada, but we want en-CA, so we need to check the language as well
                    //return new RegionInfo(c.Name).EnglishName.Equals(countryName, StringComparison.OrdinalIgnoreCase);
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

    public async Task<string> GetHomeCurrencyCodeAsync()
    {
        using var db = await _db.CreateDbContextAsync();
        var currency = await db.Settings.AsNoTracking()
            .Where(x => x.SettingKey == "currency")
            .Select(x => x.SettingValue)
            .FirstOrDefaultAsync();

        return NormalizeCurrencyCode(currency) ?? "CAD";
    }

    public async Task<List<string>> GetExtraCurrencyCodesAsync()
    {
        using var db = await _db.CreateDbContextAsync();
        var currencies = await db.Settings.AsNoTracking()
            .Where(x => x.SettingKey == "extra_currency")
            .Select(x => x.SettingValue)
            .ToListAsync();

        return currencies
            .Select(NormalizeCurrencyCode)
            .Where(x => !string.IsNullOrEmpty(x) && IsCurrencyIconSupported(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    public async Task SaveExtraCurrencyCodesAsync(IEnumerable<string> currencyCodes)
    {
        var homeCurrency = await GetHomeCurrencyCodeAsync();
        var normalized = currencyCodes
            .Select(NormalizeCurrencyCode)
            .Where(x => !string.IsNullOrEmpty(x) &&
                        IsCurrencyIconSupported(x) &&
                        !string.Equals(x, homeCurrency, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        using var db = await _db.CreateDbContextAsync();
        var existing = await db.Settings.Where(x => x.SettingKey == "extra_currency").ToListAsync();
        db.Settings.RemoveRange(existing);
        db.Settings.AddRange(normalized.Select(currency => new Setting
        {
            SettingKey = "extra_currency",
            SettingValue = currency,
            IsEnabled = 1
        }));

        await db.SaveChangesAsync();
    }

    public async Task<List<string>> GetExpenseCurrencyCodesAsync()
    {
        var homeCurrency = await GetHomeCurrencyCodeAsync();
        var currencies = new List<string>();

        if (IsCurrencyIconSupported(homeCurrency))
            currencies.Add(homeCurrency);

        var extraCurrencies = await GetExtraCurrencyCodesAsync();

        currencies.AddRange(extraCurrencies.Where(extraCurrency =>
            !string.Equals(extraCurrency, homeCurrency, StringComparison.OrdinalIgnoreCase)));

        return currencies;
    }

    public bool IsCurrencyIconSupported(string? currencyCode)
    {
        return !string.IsNullOrWhiteSpace(currencyCode) &&
               CurrencyIconCodes.Contains(currencyCode.Trim());
    }

    public string GetCurrencyCountryName(string? currencyCode)
    {
        var code = NormalizeCurrencyCode(currencyCode);
        if (code is null)
            return "";

        if (CurrencyCountryOverrides.TryGetValue(code, out var overrideName))
            return overrideName;

        if (PreferredCurrencyRegions.TryGetValue(code, out var regionCode))
        {
            var preferred = AllCountries.FirstOrDefault(c =>
                string.Equals(c.Code, regionCode, StringComparison.OrdinalIgnoreCase));
            if (preferred != null)
                return preferred.Name;
        }

        return AllCountries
            .FirstOrDefault(c => string.Equals(c.CurrencyCode, code, StringComparison.OrdinalIgnoreCase))
            ?.Name ?? "";
    }

    public CurrencyOption GetCurrencyOption(string? currencyCode)
    {
        var code = NormalizeCurrencyCode(currencyCode) ?? "";
        return new CurrencyOption(code, GetCurrencyCountryName(code));
    }

    public string GetCurrencyDisplay(string? currencyCode)
    {
        return GetCurrencyOption(currencyCode).Display;
    }

    public bool CurrencyMatchesQuery(string? currencyCode, string query)
    {
        var code = NormalizeCurrencyCode(currencyCode);
        if (code is null)
            return false;

        if (string.IsNullOrWhiteSpace(query))
            return true;

        if (code.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            GetCurrencyDisplay(code).Contains(query, StringComparison.OrdinalIgnoreCase) ||
            GetCurrencyCountryName(code).Contains(query, StringComparison.OrdinalIgnoreCase))
            return true;

        return AllCountries.Any(c =>
            string.Equals(c.CurrencyCode, code, StringComparison.OrdinalIgnoreCase) &&
            (c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             c.Code.Contains(query, StringComparison.OrdinalIgnoreCase)));
    }

    public string GetCurrencyIconForCode(string? currencyCode)
    {
        return NormalizeCurrencyCode(currencyCode) switch
        {
            "CHF" => Icons.Material.Filled.CurrencyFranc,
            "EUR" => Icons.Material.Filled.EuroSymbol,
            "GBP" => Icons.Material.Filled.CurrencyPound,
            "INR" => Icons.Material.Filled.CurrencyRupee,
            "JPY" or "CNY" => Icons.Material.Filled.CurrencyYen,
            "RUB" => Icons.Material.Filled.CurrencyRuble,
            "AUD" or "CAD" or "HKD" or "NZD" or "SGD" or "USD"
                => Icons.Material.Filled.AttachMoney,
            _ => Icons.Material.Filled.AttachMoney
        };
    }

    public string FormatAmount(decimal amount, string? currencyCode, string homeCurrencyCode, string cultureName)
    {
        var homeCurrency = NormalizeCurrencyCode(homeCurrencyCode) ?? "CAD";
        var currency = NormalizeCurrencyCode(currencyCode) ?? homeCurrency;

        if (string.Equals(currency, homeCurrency, StringComparison.OrdinalIgnoreCase))
        {
            var culture = string.IsNullOrWhiteSpace(cultureName)
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(cultureName);
            return amount.ToString("C", culture);
        }

        return $"{amount.ToString("N2", CultureInfo.InvariantCulture)} ({currency})";
    }

    private static string? NormalizeCurrencyCode(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return null;

        var code = currencyCode.Trim()
            .Split(new[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(code)
            ? null
            : code.ToUpperInvariant();
    }

    /// <summary>
    ///     Retrieves the unique installation ID. Generates a new GUID if one doesn't exist. This is used in blob management
    ///     for feedback/issues
    /// </summary>
    public async Task<string> GetUserIdAsync()
    {
        using var db = await _db.CreateDbContextAsync();
        var existing = await db.Settings.FirstOrDefaultAsync(x => x.SettingKey == "userid");

        if (existing != null)
            return existing.SettingValue;

        return await GenerateUserIdAsync();
    }

    /// <summary>
    ///     Generates and persists a unique 32-character (N format) GUID for the user.
    /// </summary>
    private async Task<string> GenerateUserIdAsync()
    {
        var guid = Guid.NewGuid().ToString("N");
        var userId = $"{guid}";

        using var db = await _db.CreateDbContextAsync();

        db.Settings.Add(new Setting
        {
            SettingKey = "userid",
            SettingValue = userId,
            IsEnabled = 1
        });

        await db.SaveChangesAsync();
        return userId;
    }

    /// <summary>
    ///     Extracts the unique ID from keys starting with "feedback-".
    /// </summary>
    public async Task<string> GetFeedbackUserIdAsync()
    {
        using var db = await _db.CreateDbContextAsync();
        var setting = await db.Settings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SettingKey.StartsWith("feedback-"));
        return setting!.SettingKey.Substring("feedback-".Length);
    }

    /// <summary>
    ///     Extracts the unique ID from keys starting with "issue-".
    /// </summary>
    public async Task<string> GetIssueUserIdAsync()
    {
        using var db = await _db.CreateDbContextAsync();
        var setting = await db.Settings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SettingKey.StartsWith("issue-"));
        return setting!.SettingKey.Substring("issue-".Length);
    }

    /// <summary>
    ///     Checks if the user has already submitted general feedback.
    /// </summary>
    public async Task<bool> IsFeedbackGiven()
    {
        using var db = await _db.CreateDbContextAsync();
        return await db.Settings.AnyAsync(x => x.SettingKey.StartsWith("feedback-"));
    }

    /// <summary>
    ///     Persists a user's feedback entry using the "feedback-{userid}" key format.
    /// </summary>
    public async Task SaveFeedbackAsync(string userId, string value)
    {
        using var db = await _db.CreateDbContextAsync();
        var setting = new Setting
        {
            SettingKey = "feedback-" + userId,
            SettingValue = value,
            IsEnabled = 1
        };
        db.Settings.Add(setting);
        await db.SaveChangesAsync();
    }

    /// <summary>
    ///     Retrieves the text content of the user's submitted feedback.
    ///     <br /><b>Business Rule:</b> Only one feedback can be submitted.
    /// </summary>
    public async Task<string> GetUserFeedbackAsync()
    {
        var result = string.Empty;

        using var db = await _db.CreateDbContextAsync();
        result = await db.Settings.Where(x => x.SettingKey.Substring(0, 8) == "feedback")
            .Select(x => x.SettingValue).FirstOrDefaultAsync() ?? string.Empty;

        return result;
    }

    /// <summary>
    ///     Saves developer response feedback to the local settings.
    /// </summary>
    public async Task SaveDevFeedbackAsync(string value)
    {
        using var db = await _db.CreateDbContextAsync();
        var existing = await db.Settings.FirstOrDefaultAsync(x => x.SettingKey == "dev_feedback");

        if (existing == null)
        {
            db.Settings.Add(new Setting
            {
                SettingKey = "dev_feedback",
                SettingValue = value,
                IsEnabled = 1
            });
            await db.SaveChangesAsync();
            return;
        }

        if (existing.SettingValue == value)
            return;

        existing.SettingValue = value;
        existing.IsEnabled = 1;
        await db.SaveChangesAsync();
    }

    /// <summary>
    ///     Retrieves the saved developer reply, including one already marked read.
    /// </summary>
    public async Task<Setting?> GetDevFeedbackAsync()
    {
        using var db = await _db.CreateDbContextAsync();
        return await db.Settings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SettingKey == "dev_feedback");
    }

    /// <summary>
    ///     Checks if a developer feedback entry exists and is enabled.
    /// </summary>
    public async Task<bool> IsDevFeedbackGiven()
    {
        using var db = await _db.CreateDbContextAsync();
        var existing =
            await db.Settings.FirstOrDefaultAsync(x => x.SettingKey == "dev_feedback" && x.IsEnabled == 1);

        if (existing != null)
            return true;

        return false;
    }

    /// <summary>
    ///     Saves a reply from the developer regarding a reported issue.
    /// </summary>
    public async Task SaveDevIssueReplyAsync(string value)
    {
        using var db = await _db.CreateDbContextAsync();

        if (!await db.Settings.AnyAsync(x => x.SettingKey == "dev_issuereply"))
        {
            var setting = new Setting
            {
                SettingKey = "dev_issuereply",
                SettingValue = value,
                IsEnabled = 1
            };
            db.Settings.Add(setting);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    ///     Persists a reported issue using the "issue-{userId}" key format.
    /// </summary>
    public async Task SaveIssueAsync(string userId, string value)
    {
        using var db = await _db.CreateDbContextAsync();
        var setting = new Setting
        {
            SettingKey = "issue-" + userId,
            SettingValue = value,
            IsEnabled = 1
        };
        db.Settings.Add(setting);
        await db.SaveChangesAsync();
    }

    /// <summary>
    ///     Checks if the user has reported at least one issue.
    /// </summary>
    public async Task<bool> IsIssueReported()
    {
        using var db = await _db.CreateDbContextAsync();
        var existing =
            await db.Settings.FirstOrDefaultAsync(x => x.SettingKey.Substring(0, 5) == "issue" && x.IsEnabled == 1);

        if (existing != null)
            return true;

        return false;
    }

    /// <summary>
    ///     Retrieves the developer's reply to a reported issue.
    /// </summary>
    public async Task<Setting> GetDevIssueReplyAsync()
    {
        if (await IsDevIssueReplied())
        {
            using var db = await _db.CreateDbContextAsync();
            return await db.Settings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.SettingKey == "dev_issuereply")!;
        }

        return null!;
    }

    /// <summary>
    ///     Checks if there is a developer reply waiting for a reported issue.
    /// </summary>
    public async Task<bool> IsDevIssueReplied()
    {
        using var db = await _db.CreateDbContextAsync();
        var existing =
            await db.Settings.FirstOrDefaultAsync(x => x.SettingKey == "dev_issuereply" && x.IsEnabled == 1);

        if (existing != null)
            return true;

        return false;
    }

    /// <summary>
    ///     Determines if a notification badge should be shown for a specific key prefix.
    /// </summary>
    public async Task<bool> ShowBadge(string value)
    {
        using var db = await _db.CreateDbContextAsync();
        var existing = await db.Settings.FirstOrDefaultAsync(x =>
            x.SettingKey.Substring(0, value.Length) == value && x.IsEnabled == 1);

        if (existing != null && !string.IsNullOrEmpty(value))
            return true;

        return false;
    }

    /// <summary>
    ///     Marks the developer reply as read. The posted feedback row stays in place.
    /// </summary>
    public async Task UpdateFeedbackAsync()
    {
        using var db = await _db.CreateDbContextAsync();
        await db.Database.ExecuteSqlRawAsync("UPDATE Settings SET IsEnabled=0 WHERE SettingKey = 'dev_feedback'");
        await db.SaveChangesAsync();
    }

    /// <summary>
    ///     Soft-deletes issue notifications by setting IsEnabled to 0 for all issue keys.
    /// </summary>
    public async Task UpdateIssueAsync()
    {
        using var db = await _db.CreateDbContextAsync();
        await db.Database.ExecuteSqlRawAsync("UPDATE Settings Set IsEnabled=0 WHERE SettingKey LIKE '%issue%'");
        await db.SaveChangesAsync();
    }

    /// <summary>
    ///     Checks which major features (Expense, Mileage, Shenshai) are currently enabled in the app.
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

        var isExpenseEnabled = Convert.ToBoolean(features
            .FirstOrDefault(x => x.SettingKey == "expensetracker")?.IsEnabled ?? 0);
        var isMileageEnabled = Convert.ToBoolean(features
            .FirstOrDefault(x => x.SettingKey == "mileagetracker")?.IsEnabled ?? 0);
        var isShenshaiEnabled = Convert.ToBoolean(features
            .FirstOrDefault(x => x.SettingKey == "shenshaicalendar")?.IsEnabled ?? 0);
        var isStepsEnabled = Convert.ToBoolean(features
            .FirstOrDefault(x => x.SettingKey == "stepstracker")?.IsEnabled ?? 0);
        return (isExpenseEnabled, isMileageEnabled, isShenshaiEnabled, isStepsEnabled);
    }

    /// <summary>
    ///     Checks if there is a new announcement that the user hasn't dismissed.
    /// </summary>
    public async Task<bool> CheckNewAnnouncement()
    {
        using var db = await _db.CreateDbContextAsync();
        return await db.Settings.AnyAsync(x => x.SettingKey == "announcement" && x.IsEnabled == 1);
    }

    /// <summary>
    ///     Marks a specific announcement ID as "seen" by setting its IsEnabled state to 0.
    /// </summary>
    public async Task SeenAnnouncementAsync(string id, bool isSeen)
    {
        using var db = await _db.CreateDbContextAsync();
        var existing = await db.Settings.FirstOrDefaultAsync(x =>
            x.SettingKey == "announcement" && x.SettingValue == id);

        if (existing == null)
        {
            db.Settings.Add(new Setting
            {
                SettingKey = "announcement",
                SettingValue = id,
                IsEnabled = 1
            });
        }
        else if (existing.IsEnabled == 1 && isSeen)
            existing.IsEnabled = 0;
        else
            return;

        await db.SaveChangesAsync();
    }

    /// <summary>
    ///     Scans trackers to determine if data exists for various dashboard widgets.
    /// </summary>
    public async Task<(bool expenseCount, bool isAutoPay, bool isMileage, bool isShenshai)> CheckDashboardCountsAsync()
    {
        using var db = await _db.CreateDbContextAsync();
        var exp = await db.ExpenseTrackers.AnyAsync(x => x.AutoPay != 1 && x.AutoPay != 0);
        var pay = await db.ExpenseTrackers.AnyAsync(x => x.AutoPay == 1);
        var mil = await db.MileageTrackers.AnyAsync();
        var she = await db.Settings.AnyAsync(x => x.SettingKey == "shenshaicalendar" && x.IsEnabled == 1);

        return (exp, pay, mil, she);
    }

    /// <summary>
    ///     Quick check to see if any main tracking features are initialized.
    /// </summary>
    public async Task<bool> CheckDashboardAsync()
    {
        using var db = await _db.CreateDbContextAsync();
        return await db.Settings.AnyAsync(x =>
            x.SettingKey == "expensetracker" ||
            x.SettingKey == "mileagetracker" ||
            x.SettingKey == "shenshaicalendar");
    }
}