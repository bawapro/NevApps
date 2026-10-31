using System.Globalization;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace NevApps.Services;

public class ReceiptService
{
    public (DateTime dateTime, string Place, decimal Price) ParseReceiptData(string rawText)
    {
        var price = 0m;
        var place = "Unknown";
        var dateTime = DateTime.Now;

        var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        var ccKeywords = new[] { "MID", "AID", "REF" };

        var isCreditCardSlip = ccKeywords.All(keyword =>
            lines.Any(line => line.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        );

        if (isCreditCardSlip)
        {
            for (var i = 0; i < lines.Length; i++)
                if (lines[i].Contains("/") || lines[i].Contains("-"))
                {
                    string[] formats = { "MM/dd/yy", "MM/dd/yyyy", "dd/MM/yy", "yyyy-MM-dd", "dd-MMM-yyyy" };

                    foreach (var format in formats)
                        if (DateTime.TryParseExact(lines[i].Trim(), format, null,
                                DateTimeStyles.None, out var parsed))
                        {
                            dateTime = parsed;
                            break;
                        }
                }
        }
        else
        {
            var dateLines = lines
                .Where(line => (
                                   line.Contains("-") ||
                                   line.Contains("/") ||
                                   line.ToLower().Contains("a.m") ||
                                   line.ToLower().Contains("am") ||
                                   line.ToLower().Contains("pm") ||
                                   line.ToLower().Contains("p.m")) &&
                               line.Contains(":") &&
                               !(line.ToUpper().Contains("TEL") || line.Contains("(") ||
                                 line.Contains(")"))
                )
                .ToList();

            if (dateLines.Count > 0)
            {
                var dt = dateLines[0].Trim().ToLower().Contains("date")
                    ? dateLines[0].Substring(dateLines[0].IndexOf(' ') + 1)
                    : dateLines[0].Trim();
                if (!DateTime.TryParse(dt, out dateTime) && !ParseReceiptDate(dt, out dateTime))
                    dateTime = DateTime.Now;
            }
        }

        var totalKeywords = new[] { "total", "amount", "balance", "sum", "grand", "due" };
        var priceRegex = new Regex(@"\b\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})\b", RegexOptions.IgnoreCase);

        var bestCandidate = 0m;
        var bestLine = "";

        foreach (var line in lines)
        {
            var matches = priceRegex.Matches(line);
            foreach (Match m in matches)
                if (decimal.TryParse(m.Value.Replace(",", "."), out var val))
                {
                    // Prefer lines with "total" keywords
                    if (totalKeywords.Any(k => line.ToLower().Contains(k)))
                    {
                        bestCandidate = val;
                        bestLine = line;
                        break; // strong match
                    }

                    if (val > bestCandidate)
                    {
                        bestCandidate = val;
                        bestLine = line;
                    }
                }
        }

        price = bestCandidate;

        return (dateTime, place, price);
    }

    private static readonly string[] GenericReceiptLines =
    [
        "takeout", "supermarket", "customer","copy"
    ];

    /// <summary>
    ///     Saved places found in the top third of the receipt, where the merchant name is printed.
    ///     A bare receipt word such as takeout is not treated as a place name.
    /// </summary>
    public IReadOnlyList<string> FindMatchingPlaces(string? rawText, IEnumerable<string>? places)
    {
        if (string.IsNullOrWhiteSpace(rawText) || places == null)
            return [];

        var lines = rawText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        if (lines.Length == 0)
            return [];

        var headerCount = Math.Max(1, (int)Math.Ceiling(lines.Length / 3d));
        lines = lines.Take(headerCount).ToArray();

        var names = places
            .Where(p => !string.IsNullOrWhiteSpace(p) && p.Trim().Length >= 3)
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matched = new List<string>();
        foreach (var name in names)
        {
            if (IsGenericReceiptLine(name))
                continue;

            var found = false;
            foreach (var line in lines)
            {
                if (line.Contains(name, StringComparison.OrdinalIgnoreCase)
                    || (IsMerchantLine(line) && name.Contains(line, StringComparison.OrdinalIgnoreCase)))
                {
                    found = true;
                    break;
                }
            }

            if (found)
                matched.Add(name);
        }

        return matched
            .OrderByDescending(name => lines.Any(line => line.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(name => name.Length)
            .ToList();
    }

    private static bool IsMerchantLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length is < 4 or > 40)
            return false;
        if (IsGenericReceiptLine(trimmed))
            return false;
        if (trimmed.Count(char.IsLetter) < 3)
            return false;
        if (trimmed.Count(char.IsDigit) > trimmed.Count(char.IsLetter))
            return false;

        return true;
    }

    private static bool IsGenericReceiptLine(string line)
    {
        var trimmed = line.Trim();
        if (GenericReceiptLines.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            return true;

        var compact = Regex.Replace(trimmed, @"[\s\-_]+", "");
        return GenericReceiptLines.Any(word =>
            Regex.Replace(word, @"[\s\-_]+", "").Equals(compact, StringComparison.OrdinalIgnoreCase));
    }

    private bool ParseReceiptDate(string dt, out DateTime dateTime)
    {
        dateTime = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(dt)) return false;

        var isParsed = false;
        var today = DateTime.Now;

        var spaceTokens = dt.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var shortMonths = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames;

        foreach (var token in spaceTokens)
        {
            if (isParsed) break;

            var cleanToken = token.Trim(',', '.');

            var hasSeparators = cleanToken.Contains("/") || cleanToken.Contains("-");
            var hasTextMonth = shortMonths.Any(m => cleanToken.Contains(m, StringComparison.OrdinalIgnoreCase));

            if (hasSeparators || hasTextMonth)
            {
                var explicitFormats = new[] { "dd/MM/yy", "MM/dd/yy", "yy/MM/dd", "dd-MM-yy", "MM-dd-yy" };
                if (DateTime.TryParseExact(cleanToken, explicitFormats, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out dateTime))
                {
                    isParsed = true;
                    break;
                }

                CultureInfo[] cultures =
                    { CultureInfo.InvariantCulture, new("en-CA"), new("en-US") };
                foreach (var culture in cultures)
                    if (DateTime.TryParse(cleanToken, culture, DateTimeStyles.None, out dateTime))
                    {
                        isParsed = true;
                        break;
                    }

                if (isParsed) break;

                var internalSeps = new[] { '/', '-', ',' };
                var subComponents = cleanToken.Split(internalSeps, StringSplitOptions.RemoveEmptyEntries);

                string foundMonth = null;
                string foundDay = null;
                string foundYear = null;

                foreach (var part in subComponents)
                {
                    var cleanPart = part.Trim();

                    if (shortMonths.Any(m => m.Equals(cleanPart, StringComparison.OrdinalIgnoreCase)))
                        foundMonth = cleanPart;
                    else if (cleanPart.Length == 4 && int.TryParse(cleanPart, out _))
                        foundYear = cleanPart;
                    else if (cleanPart.Length == 2 && foundMonth != null && int.TryParse(cleanPart, out _))
                        foundYear = cleanPart;
                    else if (int.TryParse(cleanPart, out var dayVal) && dayVal >= 1 && dayVal <= 31)
                        foundDay = cleanPart;
                }

                if (foundMonth != null && foundDay != null && foundYear != null)
                {
                    var combined = $"{foundMonth} {foundDay} {foundYear}";
                    var format = foundYear.Length == 2 ? "MMM d yy" : "MMM d yyyy";

                    if (DateTime.TryParseExact(combined, format, CultureInfo.InvariantCulture, DateTimeStyles.None,
                            out dateTime))
                    {
                        isParsed = true;
                        break;
                    }
                }
            }
        }

        if (isParsed)
        {
            if (dateTime > today)
                dateTime = new DateTime(today.Year - 1, dateTime.Month, dateTime.Day, dateTime.Hour,
                    dateTime.Minute, dateTime.Second);

            return true;
        }

        return false;
    }

    public (DateTime dateTime, string gasStation, string address, decimal rate, decimal filled, string? fuelType)
        ParseGasReceiptData(string rawText, List<string> GasStations)
    {
        var rate = 0m;
        var filled = 0m;
        var dateTime = DateTime.Now;
        var gasStation = "Unknown";
        var address = string.Empty;

        var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        gasStation = MatchGasStation(lines, GasStations);

        if (gasStation != "Unknown")
        {
            var placeIndex = Array.FindIndex(lines, line =>
                line.Contains(gasStation, StringComparison.OrdinalIgnoreCase));

            if (placeIndex == -1)
                placeIndex = Array.FindIndex(lines, line =>
                    line.Contains(gasStation.Replace(" ", "-"), StringComparison.OrdinalIgnoreCase));

            if (placeIndex != -1 && placeIndex + 1 < lines.Length)
            {
                if (lines[placeIndex + 1].Contains("*") ||
                    lines[placeIndex + 1].ToUpper().Contains("TOTAL"))
                    address = lines[placeIndex + 2];
                else
                    address = lines[placeIndex + 1];
            }
        }

        var dateLines = lines
            .Where(line =>
            {
                var lower = line.ToLower().Trim();

                var hasDateSeparator = lower.Contains("-") || lower.Contains("/") || lower.Contains(".");
                var hasTime = lower.Contains(":");
                var hasAmPm = lower.Contains("a.m") || lower.Contains("p.m") || lower.Contains("am") ||
                              lower.Contains("pm");

                var isTelOrPhone = lower.Contains("tel") || lower.Contains("(") || lower.Contains(")") ||
                                   lower.Contains("phone") || lower.Contains("fax");

                return (hasDateSeparator || hasAmPm) && !isTelOrPhone;
            })
            .ToList();

        if (dateLines.Count > 0)
        {
            // Find the best date line instead of blindly taking [0]
            var dateLine = dateLines.FirstOrDefault(line =>
                Regex.IsMatch(line.Trim(),
                    @"\b\d{4}[-./]\d{1,2}[-./]\d{1,2}\b|\b\d{1,2}[-./]\d{1,2}[-./]\d{2,4}\b"));

            var dt = dateLine.ToUpper().Replace("DATE", "").Replace(":", "")?.Trim() ?? string.Empty;
            dateTime = DateTime.MinValue;

            if (!string.IsNullOrEmpty(dt))
            {
                if (!DateTime.TryParse(dt, out dateTime)) ParseReceiptDate(dt, out dateTime); // your custom method

                // Fix future year (common with OCR on old templates or wrong year)
                if (dateTime.Year > DateTime.Now.Year + 1)
                    dateTime = new DateTime(DateTime.Now.Year, dateTime.Month, dateTime.Day);
            }
        }

        var gasFuelRegex = new Regex(@"(\d+(?:\.\d+)?)\s*L\s*AT\s*\$?\s*(\d+(?:\.\d+)?)\s*/?\s*L",
            RegexOptions.IgnoreCase);

        var match = gasFuelRegex.Match(rawText);
        if (match.Success)
        {
            decimal.TryParse(match.Groups[1].Value, out filled);
            decimal.TryParse(match.Groups[2].Value, out rate);
        }
        else
        {
            (rate, filled) = ParseFuelData(lines);
        }

        return (dateTime, gasStation, address, rate, filled, ParseFuelType(rawText));
    }

    private static string MatchGasStation(string[] lines, IEnumerable<string>? stations)
    {
        if (stations == null)
            return "Unknown";

        var text = string.Join('\n', lines);
        var folded = text.Replace("-", " ");
        string? best = null;
        var bestLength = 0;

        foreach (var station in stations)
        {
            var name = station?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
                continue;

            var foldedName = name.Replace("-", " ");
            var found = text.Contains(name, StringComparison.OrdinalIgnoreCase)
                        || folded.Contains(foldedName, StringComparison.OrdinalIgnoreCase);
            if (!found)
            {
                var words = Regex.Split(foldedName, @"[^A-Za-z0-9]+")
                    .Where(word => word.Length >= 4)
                    .ToList();
                found = words.Count > 0 && words.All(word =>
                    folded.Contains(word, StringComparison.OrdinalIgnoreCase));
            }

            if (found && name.Length > bestLength)
            {
                best = name;
                bestLength = name.Length;
            }
        }

        return best ?? "Unknown";
    }

    private static readonly HashSet<string> AddressSkipWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "street", "st", "avenue", "ave", "road", "rd", "drive", "dr", "blvd", "boulevard",
        "crescent", "lane", "way", "court", "place", "west", "east", "north", "south",
        "ontario", "canada", "unit", "the", "and"
    };

    /// <summary>
    ///     Picks a saved destination when words from the scanned address appear in that saved address.
    ///     The full address is not compared as one phrase.
    /// </summary>
    public string? MatchSavedEndLocation(string? scannedAddress, IEnumerable<string>? savedEnds)
    {
        if (string.IsNullOrWhiteSpace(scannedAddress) || savedEnds == null)
            return null;

        var scannedWords = AddressWords(scannedAddress);
        if (scannedWords.Count == 0)
            return null;

        string? best = null;
        var bestScore = 0;
        var bestRatio = 0d;

        foreach (var saved in savedEnds)
        {
            if (string.IsNullOrWhiteSpace(saved))
                continue;

            var star = saved.IndexOf('*');
            var savedAddress = star >= 0 ? saved[(star + 1)..] : saved;
            var savedWords = AddressWords(savedAddress);
            if (savedWords.Count == 0)
                continue;

            var score = scannedWords.Count(word => savedWords.Contains(word));
            if (score == 0)
                continue;

            var ratio = (double)score / savedWords.Count;
            if (score > bestScore || (score == bestScore && ratio > bestRatio))
            {
                bestScore = score;
                bestRatio = ratio;
                best = saved;
            }
        }

        return best;
    }

    private static HashSet<string> AddressWords(string text)
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in Regex.Split(text, @"[^A-Za-z0-9]+"))
        {
            if (string.IsNullOrWhiteSpace(part) || AddressSkipWords.Contains(part))
                continue;

            var isNumber = part.All(char.IsDigit);
            if (isNumber && part.Length >= 2)
                words.Add(part);
            else if (!isNumber && part.Length >= 4)
                words.Add(part);
        }

        return words;
    }

    /// <summary>
    ///     Reads the grade printed on a gas receipt. Regular is 87, Plus is 89, Premium is 91.
    ///     The grade word is enough. A nearby octane number replaces the default for that word.
    /// </summary>
    private static string? ParseFuelType(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return null;

        var text = rawText.ToUpperInvariant();
        text = Regex.Replace(text, @"REGU[IL1][A4]R", "REGULAR");
        text = Regex.Replace(text, @"PREM[I1]UM", "PREMIUM");
        text = Regex.Replace(text, @"UN[IL1]EAD\w*", "UNLEADED");

        var letters = Regex.Replace(text, @"[^A-Z0-9]", "");

        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var only = Regex.Match(line.Trim(), @"^(87|89|91|93|94)$");
            if (only.Success)
                return only.Groups[1].Value;
        }

        if (letters.Contains("DIESEL"))
            return "D";

        if (letters.Contains("PREMIUM") || letters.Contains("VPOWER") || letters.Contains("SUPREME"))
            return letters.Contains("94") ? "94" : letters.Contains("93") ? "93" : "91";

        if (letters.Contains("REGULARPLUS") || letters.Contains("MIDGRADE") || Regex.IsMatch(text, @"\bPLUS\b"))
            return "89";

        if (letters.Contains("REGULAR") || letters.Contains("UNLEADED") || letters.Contains("UNLD") ||
            Regex.IsMatch(text, @"\bREG\b"))
            return "87";

        return null;
    }

    private (decimal rate, decimal filled) ParseFuelData(string[] lines)
    {
        decimal litres = 0;
        decimal pricePerLitre = 0;

        var fuelLines = lines
            .Select(line => line.Replace("$", "").Trim())
            .Where(line =>
            {
                if (string.IsNullOrWhiteSpace(line)) return false;

                var clean = line;

                if (!clean.Contains(".") || !clean.Contains(" "))
                    return false;

                // Allow digits, space, dot only
                var isValid = clean.All(c => char.IsDigit(c) || c == ' ' || c == '.');

                return isValid;
            })
            .ToList();

        if (fuelLines.Count == 1)
        {
            var d1 = decimal.Parse(fuelLines[0].Split(' ')[0]);
            var d2 = decimal.Parse(fuelLines[0].Split(' ')[1]);
            if (d1 < d2)
            {
                pricePerLitre = d1;
                litres = d2;
            }
            else
            {
                pricePerLitre = d2;
                litres = d1;
            }
        }
        else
        {
            var numericLines = lines
                .Select(line => line.Replace("$", "").Trim())
                .Where(line => line.Contains(".") && decimal.TryParse(line, out _))
                .Select(line => decimal.Parse(line))
                .Take(3)
                .ToList();

            if (numericLines.Count >= 2)
            {
                var totalCost = numericLines.Max();
                pricePerLitre = numericLines.Min();

                if (pricePerLitre > 0)
                    litres = totalCost / pricePerLitre;
            }
        }

        return (pricePerLitre, litres);
    }

    public Task<byte[]> PreprocessImageForOcrAsync(byte[] rawImageBytes)
    {
        return Task.Run(() => PreprocessImageForOcr(rawImageBytes));
    }

    private static byte[] PreprocessImageForOcr(byte[] rawImageBytes)
    {
        using var original = SKBitmap.Decode(rawImageBytes);
        if (original == null) return rawImageBytes;

        // Resize (keep under 1280–1500px for speed + accuracy)
        var maxDim = 1400;
        var scale = Math.Min((float)maxDim / original.Width, (float)maxDim / original.Height);
        var newW = scale < 1 ? (int)(original.Width * scale) : original.Width;
        var newH = scale < 1 ? (int)(original.Height * scale) : original.Height;

        using var resized = new SKBitmap(new SKImageInfo(newW, newH, SKColorType.Gray8));
        original.ScalePixels(resized, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

        //Improve contrast & sharpness ===
        using var sharpened = new SKBitmap(resized.Info);
        using var canvas = new SKCanvas(sharpened);

        // High contrast + slight sharpen
        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new[]
            {
                1.2f, 0, 0, 0, 0, // Red
                0, 1.2f, 0, 0, 0, // Green
                0, 0, 1.2f, 0, 0, // Blue
                0, 0, 0, 1, 0 // Alpha
            })
        };

        canvas.DrawBitmap(resized, 0, 0, paint);

        using var image = SKImage.FromBitmap(sharpened);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 88);

        return data.ToArray();
    }

    public async Task<byte[]> ResizeAndConvertToJpegAsync(Stream inputStream,
        int maxWidth = 1200, int jpegQuality = 85)
    {
        if (inputStream.CanSeek)
            inputStream.Position = 0;

        using var memoryStream = new MemoryStream();
        await inputStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var originalBitmap = SKBitmap.Decode(memoryStream);

        if (originalBitmap == null)
            return Array.Empty<byte>();

        // Resize while keeping aspect ratio
        var newWidth = originalBitmap.Width;
        var newHeight = originalBitmap.Height;

        if (newWidth > maxWidth)
        {
            var ratio = (float)maxWidth / newWidth;
            newWidth = maxWidth;
            newHeight = (int)(originalBitmap.Height * ratio);
        }

        using var resizedBitmap = originalBitmap.Resize(
            new SKImageInfo(newWidth, newHeight),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

        using var outputStream = new MemoryStream();
        resizedBitmap.Encode(outputStream, SKEncodedImageFormat.Jpeg, jpegQuality);

        return outputStream.ToArray();
    }

    public async Task<FileResult> ConvertBytesToFileResult(byte[] imageBytes, string originalFileName,
        string contentType = "image/jpeg")
    {
        // 1. Get the device's secure temporary cache directory
        var tempFolder = FileSystem.CacheDirectory;

        // 2. Generate a clean, unique file path using the original extension
        var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
        var fullPath = Path.Combine(tempFolder, uniqueFileName);

        // 3. Write the cropped/compressed raw bytes straight to disk
        await File.WriteAllBytesAsync(fullPath, imageBytes);

        // 4. Return a new FileResult targeting our optimized temp file
        return new FileResult(fullPath, contentType);
    }
}