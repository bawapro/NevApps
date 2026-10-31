using System.Globalization;
using System.Text.RegularExpressions;
using Plugin.Maui.OCR;
using SkiaSharp;
namespace NevApps.Services;
public enum ReceiptScan { Expense, Fuel, Ev }
public interface IReceiptScanService
{
    Task<ScanReceiptResult> ScanAsync(ReceiptScanOptions options, CancellationToken ct = default);
}
public sealed record ReceiptScanOptions(
    ReceiptScan Scan,
    IEnumerable<string>? KnownPlaces = null,
    IEnumerable<string>? KnownStations = null,
    bool UseHighAccuracyOcr = true);
public sealed record ScanReceiptResult(
    bool Success,
    string? ErrorMessage,
    string? RawOcrText,
    byte[]? ProcessedImageBytes,
    ReceiptParseResult? Receipt,
    MileageReceiptResult? Mileage);
public sealed record ReceiptParseResult(
    DateTime? Date,
    string? Place,
    decimal? Total,
    string? Address,
    string? Phone,
    IReadOnlyList<ReceiptLineItem> Items,
    IReadOnlyList<string> MatchedPlaces);
public sealed record ReceiptLineItem(string Description, decimal? Quantity, decimal Amount);
// Shared by fuel and EV. The form shows these as the station, type, rate, amount filled, and price.
// Mileage is efficiency. A receipt does not print it; the form computes it from distance.
public sealed record MileageReceiptResult(
    ReceiptScan Scan,
    DateTime Date,
    string? Station,
    string? Address,
    decimal Rate,
    decimal Filled,
    decimal? Price,
    string? Type,
    decimal? Mileage = null);
public sealed class ReceiptScanService : IReceiptScanService
{
    private readonly IOcrService _ocr;
    public ReceiptScanService(IOcrService ocr) => _ocr = ocr;
    // ---------- Compiled patterns ----------
    private static readonly Regex MoneyRx = new(
        @"\b\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})\b", RegexOptions.Compiled);
    private static readonly Regex DateRx = new(
        @"\b\d{4}[-/.]\d{1,2}[-/.]\d{1,2}\b" +
        @"|\b\d{1,2}[-/.]\d{1,2}[-/.]\d{2,4}\b" +
        @"|\b[A-Za-z]{3,9}\.?\s+\d{1,2},?\s+\d{4}\b" +
        @"|\b\d{1,2}[-\s][A-Za-z]{3,9}[-\s]\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex TimeRx = new(
        @"\b\d{1,2}:\d{2}(?::\d{2})?\s*(?:[AaPp]\.?[Mm]\.?)?", RegexOptions.Compiled);
    private static readonly Regex PhoneRx = new(
        @"(?:\+?\d{1,2}[\s.-])?(?:\(?\d{3}\)?[\s.-])\d{3}[\s.-]\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex AddressRx = new(
        @"\b\d{1,6}\s+[A-Za-z0-9][A-Za-z0-9.'-]{1,30}(?:\s+[A-Za-z0-9.'-]{1,30}){0,5}\s+" +
        @"(?:Street|St|Avenue|Ave|Road|Rd|Drive|Dr|Boulevard|Blvd|Lane|Ln|Way|Court|Ct|Crescent|Cres|Place|Pl)\b\.?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex QtyRx = new(
        @"^(\d+(?:[.,]\d+)?)\s*[xX@]\s*", RegexOptions.Compiled);
    // Fuel
    private static readonly Regex FuelRx = new(
        @"(\d+(?:\.\d+)?)\s*L\s*AT\s*\$?\s*(\d+(?:\.\d+)?)\s*/?\s*L",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // EV
    private static readonly Regex KwhRx = new(
        @"(?<![/@$\d.])(\d+(?:\.\d+)?)\s*kwh", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex KwhRateRx = new(
        @"\$?(\d+\.\d+)\s*/\s*kwh|kwh\s*@\s*\$?(\d+\.\d+)|@\s*\$?(\d+\.\d+)\s*/?\s*kwh",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ChargeTypeRx = new(
        @"\b(DC\s*Fast|DCFC|Supercharg\w*|Fast\s*Charg\w*|AC\s*Level\s*[12]|Level\s*[123]|L[123]|DC)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // ---------- Keyword lists ----------
    private static readonly string[] TotalWords =
        ["total", "amount", "balance", "sum", "grand", "due"];
    private static readonly string[] ItemSkipWords =
    [
        "total", "subtotal", "sub total", "tax", "change", "balance", "amount due", "amount",
        "cash", "visa", "mastercard", "debit", "credit", "tip", "gst", "hst", "pst", "tender", "savings"
    ];
    private static readonly string[] GenericWords = ["takeout", "supermarket", "customer", "copy"];
    private static readonly CultureInfo[] DateCultures =
        [CultureInfo.InvariantCulture, new CultureInfo("en-CA"), new CultureInfo("en-US")];
    // ======================================================================
    // Public entry point
    // ======================================================================
    public async Task<ScanReceiptResult> ScanAsync(ReceiptScanOptions options, CancellationToken ct = default)
    {
        try
        {
            if (!await EnsureCameraPermissionAsync())
                return Fail("Camera permission denied");
            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions());
            if (photo == null)
                return Fail("Capture cancelled");
            var raw = await ReadAllBytesAsync(photo, ct);
            var image = await Task.Run(() => PreprocessImage(raw, ct), ct);
            var text = await RecognizeAsync(image, options.UseHighAccuracyOcr, ct);
            if (string.IsNullOrWhiteSpace(text))
                return Fail("No text detected", text, image);
            var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim())
                            .Where(l => l.Length > 0)
                            .ToArray();
            return options.Scan switch
            {
                ReceiptScan.Fuel => Ok(text, image, mileage: ParseFuel(text, lines, options.KnownStations)),
                ReceiptScan.Ev => Ok(text, image, mileage: ParseEv(text, lines, options.KnownStations)),
                ReceiptScan.Expense => Ok(text, image, receipt: ParseExpense(text, lines, options.KnownPlaces)),
            };
        }
        catch (OperationCanceledException)
        {
            return Fail("Scan cancelled");
        }
        catch (Exception ex)
        {
            return Fail($"Scan failed: {ex.Message}");
        }
    }
    // ======================================================================
    // OCR
    // ======================================================================
    private async Task<string?> RecognizeAsync(byte[] image, bool tryHard, CancellationToken ct)
    {
        await _ocr.InitAsync(ct);
        var options = new OcrOptions.Builder()
            .SetLanguage("en-US")
            .SetTryHard(tryHard)
            .Build();
        var recognized = await _ocr.RecognizeTextAsync(image, options, ct);
        if (recognized is not { Success: true })
            return recognized?.AllText;
        return recognized.Lines is { Count: > 0 }
            ? string.Join('\n', recognized.Lines)
            : recognized.AllText;
    }
    private static byte[] PreprocessImage(byte[] raw, CancellationToken ct)
    {
        using var original = SKBitmap.Decode(raw);
        if (original is null) return raw;
        ct.ThrowIfCancellationRequested();
        const int maxDim = 1400;
        var scale = Math.Min(1f, maxDim / (float)Math.Max(original.Width, original.Height));
        var w = (int)(original.Width * scale);
        var h = (int)(original.Height * scale);
        using var gray = new SKBitmap(new SKImageInfo(w, h, SKColorType.Gray8));
        original.ScalePixels(gray, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        AutoContrastStretch(gray);
        using var image = SKImage.FromBitmap(gray);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        return data.ToArray();
    }
    private static void AutoContrastStretch(SKBitmap gray)
    {
        var span = gray.GetPixelSpan();   // verify this is writable in your SkiaSharp version
        int w = gray.Width, h = gray.Height, stride = gray.RowBytes;
        if (w == 0 || h == 0 || span.Length < h * stride) return;
        // Histogram from every other row: statistically sufficient, half the work
        var hist = new int[256];
        for (var y = 0; y < h; y += 2)
            foreach (var v in span.Slice(y * stride, w))
                hist[v]++;
        var sampled = w * ((h + 1) / 2);
        var clip = Math.Max(1, sampled / 100);
        int lo = 0, hi = 255, acc = 0;
        for (var i = 0; i < 256; i++) { acc += hist[i]; if (acc >= clip) { lo = i; break; } }
        acc = 0;
        for (var i = 255; i >= 0; i--) { acc += hist[i]; if (acc >= clip) { hi = i; break; } }
        if (hi - lo < 16) return;   // already well-spread; skip the pass
        var k = 255f / (hi - lo);
        for (var y = 0; y < h; y++)
        {
            var row = span.Slice(y * stride, w);
            for (var x = 0; x < w; x++)
                row[x] = (byte)Math.Clamp((int)((row[x] - lo) * k), 0, 255);
        }
    }
    // ======================================================================
    // Expense
    // ======================================================================
    private static ReceiptParseResult ParseExpense(string text, string[] lines, IEnumerable<string>? knownPlaces)
    {
        var places = MatchPlaces(lines, knownPlaces);
        return new ReceiptParseResult(
            Date: ParseDate(text),
            Place: places.FirstOrDefault(),
            Total: ParseTotal(lines, keywordOnly: false),
            Address: FindAddress(lines, station: null),
            Phone: PhoneRx.Match(text) is { Success: true } p ? p.Value.Trim() : null,
            Items: ParseItems(lines),
            MatchedPlaces: places);
    }
    private static List<string> MatchPlaces(string[] lines, IEnumerable<string>? places)
    {
        if (places is null || lines.Length == 0) return [];
        // Merchant name lives in the top third of the receipt
        var header = lines.Take(Math.Max(1, (int)Math.Ceiling(lines.Length / 3d))).ToArray();
        var matched = places
            .Where(p => !string.IsNullOrWhiteSpace(p) && p.Trim().Length >= 3)
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(name => !IsGenericLine(name))
            .Where(name => header.Any(line =>
                line.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                (IsMerchantLine(line) && name.Contains(line, StringComparison.OrdinalIgnoreCase))))
            .ToList();
        return matched
            .OrderByDescending(n => header.Any(l => l.Equals(n, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(n => n.Length)
            .ToList();
    }
    private static bool IsGenericLine(string line)
    {
        var compact = Regex.Replace(line.Trim(), @"[\s\-_]+", "");
        return GenericWords.Any(w => w.Equals(compact, StringComparison.OrdinalIgnoreCase));
    }
    private static bool IsMerchantLine(string line)
    {
        var t = line.Trim();
        if (t.Length is < 4 or > 40) return false;
        if (IsGenericLine(t)) return false;
        if (t.Count(char.IsLetter) < 3) return false;
        return t.Count(char.IsDigit) <= t.Count(char.IsLetter);
    }
    private static IReadOnlyList<ReceiptLineItem> ParseItems(string[] lines)
    {
        var items = new List<ReceiptLineItem>();
        foreach (var line in lines)
        {
            if (ItemSkipWords.Any(w => line.Contains(w, StringComparison.OrdinalIgnoreCase)))
                continue;
            var m = MoneyRx.Match(line);
            if (!m.Success || !TryMoney(m.Value, out var amount))
                continue;
            var desc = (line[..m.Index] + line[(m.Index + m.Length)..]).Trim(' ', '-', '.', ':', '*');
            decimal? qty = null;
            var q = QtyRx.Match(desc);
            if (q.Success && decimal.TryParse(q.Groups[1].Value.Replace(',', '.'),
                    NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedQty))
            {
                qty = parsedQty;
                desc = desc[q.Length..].Trim();
            }
            if (desc.Count(char.IsLetter) < 2)
                continue;
            items.Add(new ReceiptLineItem(desc, qty, amount));
        }
        return items;
    }
    // ======================================================================
    // Fuel
    // ======================================================================
    private static MileageReceiptResult ParseFuel(string text, string[] lines, IEnumerable<string>? stations)
    {
        var station = MatchStation(text, stations);
        decimal rate = 0, filled = 0;
        var m = FuelRx.Match(text);
        if (m.Success)
        {
            decimal.TryParse(m.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out filled);
            decimal.TryParse(m.Groups[2].Value, NumberStyles.Number, CultureInfo.In­variantCulture, out rate);
        }
        else
        {
            (rate, filled) = ParseFuelData(lines);
        }
        var cost = ParseTotal(lines, keywordOnly: false)
                   ?? (rate > 0 && filled > 0 ? Math.Round(rate * filled, 2) : null);
        return new MileageReceiptResult(
            Scan: ReceiptScan.Fuel,
            Date: ParseDate(text) ?? DateTime.Now,
            Station: station,
            Address: FindAddress(lines, station),
            Rate: rate,
            Filled: filled,
            Price: cost,
            Type: FuelGrade(text));
    }
    private static (decimal rate, decimal filled) ParseFuelData(string[] lines)
    {
        var cleaned = lines.Select(l => l.Replace("$", "").Trim()).ToArray();
        // Single "price litres" line, e.g. "1.459 42.310"
        var pairs = cleaned.Where(l => l.Contains('.') && l.Contains(' ') &&
                                       l.All(c => char.IsDigit(c) || c == ' ' || c == '.'))
                           .ToList();
        if (pairs.Count == 1)
        {
            var parts = pairs[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 &&
                decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var d1) &&
                decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var d2))
            {
                return d1 < d2 ? (d1, d2) : (d2, d1);   // (rate, filled)
            }
            return (0, 0);
        }
        // Fallback: total and rate printed separately; litres = total / rate
        var nums = cleaned.Where(l => l.Contains('.') &&
                                      decimal.TryParse(l, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                          .Select(l => decimal.Parse(l, NumberStyles.Number, CultureInfo.InvariantCulture))
                          .Take(3)
                          .ToList();
        if (nums.Count < 2) return (0, 0);
        var rate = nums.Min();
        var cost = nums.Max();
        return (rate, rate > 0 ? Math.Round(cost / rate, 3) : 0);
    }
    private static string? FuelGrade(string rawText)
    {
        var t = rawText.ToUpperInvariant();
        t = Regex.Replace(t, @"REGU[IL1][A4]R", "REGULAR");
        t = Regex.Replace(t, @"PREM[I1]UM", "PREMIUM");
        t = Regex.Replace(t, @"UN[IL1]EAD\w*", "UNLEADED");
        // Explicit octane number on its own line wins
        foreach (var line in t.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            if (Regex.Match(line.Trim(), @"^(87|89|91|93|94)$") is { Success: true } o)
                return o.Groups[1].Value;
        var letters = Regex.Replace(t, @"[^A-Z0-9]", "");
        if (letters.Contains("DIESEL")) return "D";
        if (letters.Contains("PREMIUM") || letters.Contains("VPOWER") || letters.Contains("SUPREME"))
            return Regex.IsMatch(t, @"\b94\b") ? "94" : Regex.IsMatch(t, @"\b93\b") ? "93" : "91";
        if (letters.Contains("REGULARPLUS") || letters.Contains("MIDGRADE") || Regex.IsMatch(t, @"\bPLUS\b"))
            return "89";
        if (letters.Contains("REGULAR") || letters.Contains("UNLEADED") || letters.Contains("UNLD") ||
            Regex.IsMatch(t, @"\bREG\b"))
            return "87";
        return null;
    }
    // ======================================================================
    // EV
    // ======================================================================
    private static MileageReceiptResult ParseEv(string text, string[] lines, IEnumerable<string>? stations)
    {
        var station = MatchStation(text, stations);
        decimal kwh = 0;
        var kwhM = KwhRx.Match(text);
        if (kwhM.Success)
            decimal.TryParse(kwhM.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out kwh);
        decimal rate = 0;
        var rateM = KwhRateRx.Match(text);
        if (rateM.Success)
        {
            var raw = rateM.Groups.Cast<Group>().Skip(1).First(g => g.Success).Value;
            decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out rate);
        }
        // Keyword lines only: avoids picking up "0.35/kWh" or "23.45 kWh" as a cost
        var cost = ParseTotal(lines, keywordOnly: true);
        // Any two of (kWh, rate, cost) determine the third
        if (kwh > 0 && rate > 0 && cost is null)
            cost = Math.Round(kwh * rate, 2);
        else if (kwh > 0 && rate == 0 && cost is > 0)
            rate = Math.Round(cost.Value / kwh, 4);
        else if (rate > 0 && kwh == 0 && cost is > 0)
            kwh = Math.Round(cost.Value / rate, 3);
        return new MileageReceiptResult(
            Scan: ReceiptScan.Ev,
            Date: ParseDate(text) ?? DateTime.Now,
            Station: station,
            Address: FindAddress(lines, station),
            Rate: rate,
            Filled: kwh,
            Price: cost,
            Type: ChargeType(text));
    }
    // ======================================================================
    // Shared helpers
    // ======================================================================
    private static string? ChargeType(string text)
    {
        var match = ChargeTypeRx.Match(text);
        if (!match.Success)
            return null;

        var value = Regex.Replace(match.Value, @"[^A-Za-z0-9]", "").ToUpperInvariant();
        if (value is "L1" or "LEVEL1" or "ACLEVEL1")
            return "L1";
        if (value is "L2" or "LEVEL2" or "ACLEVEL2")
            return "L2";
        return "DC";
    }

    private static decimal? ParseTotal(string[] lines, bool keywordOnly)
    {
        var keyed = new List<decimal>();
        var all = new List<decimal>();
        foreach (var line in lines)
        {
            if (line.Contains("kwh", StringComparison.OrdinalIgnoreCase))
                continue;
            var isTotal = TotalWords.Any(k => line.Contains(k, StringComparison.OrdinalIgnoreCase));
            foreach (Match m in MoneyRx.Matches(line))
            {
                if (!TryMoney(m.Value, out var v)) continue;
                all.Add(v);
                if (isTotal) keyed.Add(v);
            }
        }
        var pool = keyed.Count > 0 || keywordOnly ? keyed : all;
        return pool.Count > 0 ? pool.Max() : null;
    }
    private static string? MatchStation(string text, IEnumerable<string>? stations)
    {
        if (stations is null) return null;
        var folded = text.Replace("-", " ");
        string? best = null;
        foreach (var station in stations)
        {
            var name = station?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3) continue;
            var foldedName = name.Replace("-", " ");
            var found = text.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                        folded.Contains(foldedName, StringComparison.OrdinalIgnoreCase);
            if (!found)
            {
                var words = Regex.Split(foldedName, @"[^A-Za-z0-9]+").Where(w => w.Length >= 4).ToList();
                found = words.Count > 0 &&
                        words.All(w => folded.Contains(w, StringComparison.OrdinalIgnoreCase));
            }
            if (found && (best is null || name.Length > best.Length))
                best = name;
        }
        return best;
    }
    private static string? FindAddress(string[] lines, string? station)
    {
        foreach (var line in lines)
            if (AddressRx.Match(line) is { Success: true } m)
                return m.Value.Trim().TrimEnd('.');
        if (station is null) return null;
        // Fallback: the line after the station name (skipping "*" or "TOTAL" lines)
        var idx = Array.FindIndex(lines, l => l.Contains(station, StringComparison.OrdinalIgnoreCase));
        if (idx < 0 || idx + 1 >= lines.Length) return null;
        var next = lines[idx + 1];
        var pick = next.Contains('*') || next.Contains("TOTAL", StringComparison.OrdinalIgnoreCase)
            ? idx + 2
            : idx + 1;
        return pick < lines.Length ? lines[pick] : null;
    }
    private static DateTime? ParseDate(string text)
    {
        var dateM = DateRx.Match(text);
        if (!dateM.Success) return null;
        var timeM = TimeRx.Match(text);
        var candidates = timeM.Success
            ? new[] { $"{dateM.Value} {timeM.Value}", dateM.Value }
            : new[] { dateM.Value };
        foreach (var candidate in candidates)
            foreach (var culture in DateCultures)
                if (DateTime.TryParse(candidate, culture, DateTimeStyles.None, out var dt))
                    return Normalize(dt);
        if (DateTime.TryParseExact(dateM.Value,
                new[] { "dd-MMM-yyyy", "d-MMM-yyyy", "dd MMM yyyy", "MMM d yyyy", "MMM d, yyyy" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return Normalize(exact);
        return null;
    }
    // Receipts can't be in the future; OCR misreads sometimes push the year forward
    private static DateTime Normalize(DateTime dt) =>
        dt > DateTime.Now.AddDays(1) ? dt.AddYears(-1) : dt;
    private static bool TryMoney(string text, out decimal value)
    {
        var t = text.Trim();
        var lastComma = t.LastIndexOf(',');
        var lastDot = t.LastIndexOf('.');
        if (lastComma >= 0 && lastDot >= 0)
            t = lastComma > lastDot ? t.Replace(".", "").Replace(',', '.') : t.Replace(",", "");
        else if (lastComma >= 0)
            t = t.Length - lastComma - 1 == 2 ? t.Replace(',', '.') : t.Replace(",", "");
        return decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
    private static ScanReceiptResult Ok(string text, byte[] image,
        ReceiptParseResult? receipt = null, MileageReceiptResult? mileage = null) =>
        new(true, null, text, image, receipt, mileage);
    private static ScanReceiptResult Fail(string message, string? text = null, byte[]? image = null) =>
        new(false, message, text, image, null, null);
    private static async Task<bool> EnsureCameraPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status == PermissionStatus.Granted) return true;
        status = await Permissions.RequestAsync<Permissions.Camera>();
        return status == PermissionStatus.Granted;
    }
    private static async Task<byte[]> ReadAllBytesAsync(FileResult photo, CancellationToken ct)
    {
        await using var stream = await photo.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
