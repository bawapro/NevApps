using System.Globalization;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace NevApps.Services;

public class ReceiptService
{
    public (DateTime dateTime, string Place, decimal Price) ParseReceiptData(string rawText,
        List<string> Places)
    {
        decimal price = 0m;
        string place = "Unknown";
        DateTime dateTime = DateTime.Now;

        var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        var ccKeywords = new[] { "MID", "AID", "REF" };

        bool isCreditCardSlip = ccKeywords.All(keyword =>
            lines.Any(line => line.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        );

        if (isCreditCardSlip)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("/") || lines[i].Contains("-"))
                {
                    string[] formats = { "MM/dd/yy", "MM/dd/yyyy", "dd/MM/yy", "yyyy-MM-dd", "dd-MMM-yyyy" };

                    foreach (var format in formats)
                    {
                        if (DateTime.TryParseExact(lines[i].Trim(), format, null,
                                DateTimeStyles.None, out DateTime parsed))
                        {
                            dateTime = parsed;
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < 5; i++)
            {
                if (!ccKeywords.Any(k => lines[i].Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    string placeMatch =
                        Places.FirstOrDefault(place => place.Contains(lines[i], StringComparison.OrdinalIgnoreCase));

                    if (placeMatch == null)
                    {
                        placeMatch = Places.FirstOrDefault(place =>
                            lines[i].Contains(place.Trim(), StringComparison.OrdinalIgnoreCase));
                    }

                    if (placeMatch != null)
                    {
                        place = placeMatch;
                        break;
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < 10; i++)
            {
                string placeMatch =
                    Places.FirstOrDefault(place => place.Contains(lines[i], StringComparison.OrdinalIgnoreCase));

                if (placeMatch == null)
                {
                    placeMatch = Places.FirstOrDefault(place =>
                        lines[i].Contains(place.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (placeMatch != null)
                {
                    place = placeMatch;
                    break;
                }
            }

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
                if (!DateTime.TryParse(dt, out dateTime))
                {
                    ParseReceiptDate(dt, out dateTime);
                }
            }
        }

        var totalKeywords = new[] { "total", "amount", "balance", "sum", "grand", "due" };
        var priceRegex = new Regex(@"\b\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})\b", RegexOptions.IgnoreCase);

        decimal bestCandidate = 0m;
        string bestLine = "";

        foreach (var line in lines)
        {
            var matches = priceRegex.Matches(line);
            foreach (Match m in matches)
            {
                if (decimal.TryParse(m.Value.Replace(",", "."), out decimal val))
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
        }

        price = bestCandidate;

        return (dateTime, place, price);
    }

    private bool ParseReceiptDate(string dt, out DateTime dateTime)
    {
        dateTime = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(dt)) return false;

        bool isParsed = false;
        DateTime today = DateTime.Now;

        string[] spaceTokens = dt.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string[] shortMonths = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames;

        foreach (var token in spaceTokens)
        {
            if (isParsed) break;

            string cleanToken = token.Trim(',', '.');

            bool hasSeparators = cleanToken.Contains("/") || cleanToken.Contains("-");
            bool hasTextMonth = shortMonths.Any(m => cleanToken.Contains(m, StringComparison.OrdinalIgnoreCase));

            if (hasSeparators || hasTextMonth)
            {
                string[] explicitFormats = new string[] { "dd/MM/yy", "MM/dd/yy", "yy/MM/dd", "dd-MM-yy", "MM-dd-yy" };
                if (DateTime.TryParseExact(cleanToken, explicitFormats, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out dateTime))
                {
                    isParsed = true;
                    break;
                }

                CultureInfo[] cultures =
                    { CultureInfo.InvariantCulture, new CultureInfo("en-CA"), new CultureInfo("en-US") };
                foreach (var culture in cultures)
                {
                    if (DateTime.TryParse(cleanToken, culture, DateTimeStyles.None, out dateTime))
                    {
                        isParsed = true;
                        break;
                    }
                }

                if (isParsed) break;

                char[] internalSeps = new char[] { '/', '-', ',' };
                string[] subComponents = cleanToken.Split(internalSeps, StringSplitOptions.RemoveEmptyEntries);

                string foundMonth = null;
                string foundDay = null;
                string foundYear = null;

                foreach (var part in subComponents)
                {
                    string cleanPart = part.Trim();

                    if (shortMonths.Any(m => m.Equals(cleanPart, StringComparison.OrdinalIgnoreCase)))
                        foundMonth = cleanPart;
                    else if (cleanPart.Length == 4 && int.TryParse(cleanPart, out _))
                        foundYear = cleanPart;
                    else if (cleanPart.Length == 2 && foundMonth != null && int.TryParse(cleanPart, out _))
                        foundYear = cleanPart;
                    else if (int.TryParse(cleanPart, out int dayVal) && dayVal >= 1 && dayVal <= 31)
                        foundDay = cleanPart;
                }

                if (foundMonth != null && foundDay != null && foundYear != null)
                {
                    string combined = $"{foundMonth} {foundDay} {foundYear}";
                    string format = foundYear.Length == 2 ? "MMM d yy" : "MMM d yyyy";

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
            {
                dateTime = new DateTime(today.Year - 1, dateTime.Month, dateTime.Day, dateTime.Hour,
                    dateTime.Minute, dateTime.Second);
            }

            return true;
        }

        return false;
    }

    public (DateTime dateTime, string gasStation, string address, decimal rate, decimal filled)
        ParseGasReceiptData(string rawText, List<string> GasStations)
    {
        decimal rate = 0m;
        decimal filled = 0m;
        DateTime dateTime = DateTime.Now;
        string gasStation = "Unknown";
        string address = string.Empty;

        var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        for (int i = 0; i < lines.Length / 2; i++)
        {
            string placeMatch =
                GasStations.FirstOrDefault(place =>
                    place.Contains(lines[i].Replace("-", " "), StringComparison.OrdinalIgnoreCase));

            if (placeMatch == null)
            {
                placeMatch = GasStations.FirstOrDefault(place =>
                    lines[i].Contains(place.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (placeMatch != null)
            {
                gasStation = placeMatch;
                break;
            }
        }

        if (gasStation != "Unknown")
        {
            int placeIndex = Array.FindIndex(lines, line =>
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
                string lower = line.ToLower().Trim();

                bool hasDateSeparator = lower.Contains("-") || lower.Contains("/") || lower.Contains(".");
                bool hasTime = lower.Contains(":");
                bool hasAmPm = lower.Contains("a.m") || lower.Contains("p.m") || lower.Contains("am") ||
                               lower.Contains("pm");

                bool isTelOrPhone = lower.Contains("tel") || lower.Contains("(") || lower.Contains(")") ||
                                    lower.Contains("phone") || lower.Contains("fax");

                return (hasDateSeparator || hasAmPm) && !isTelOrPhone;
            })
            .ToList();

        if (dateLines.Count > 0)
        {
            // Find the best date line instead of blindly taking [0]
            var dateLine = dateLines.FirstOrDefault(line =>
                System.Text.RegularExpressions.Regex.IsMatch(line.Trim(),
                    @"\b\d{4}[-./]\d{1,2}[-./]\d{1,2}\b|\b\d{1,2}[-./]\d{1,2}[-./]\d{2,4}\b"));

            string dt = dateLine.ToUpper().Replace("DATE","").Replace(":","")?.Trim() ?? string.Empty;
            dateTime = DateTime.MinValue;

            if (!string.IsNullOrEmpty(dt))
            {
                if (!DateTime.TryParse(dt, out dateTime))
                {
                    ParseReceiptDate(dt, out dateTime);
                }

                // Fix future year (common with OCR on old templates or wrong year)
                if (dateTime.Year > DateTime.Now.Year + 1)
                {
                    dateTime = new DateTime(DateTime.Now.Year, dateTime.Month, dateTime.Day);
                }
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

        return (dateTime, gasStation, address, rate, filled);
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
        
                string clean = line;
        
                if (!clean.Contains(".") || !clean.Contains(" ")) 
                    return false;

                // Allow digits, space, dot only
                bool isValid = clean.All(c => char.IsDigit(c) || c == ' ' || c == '.');

                return isValid;
            })
            .ToList();

        if (fuelLines.Count == 1)
        {
            decimal d1 = decimal.Parse(fuelLines[0].Split(' ')[0]);
            decimal d2 = decimal.Parse(fuelLines[0].Split(' ')[1]);
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
                decimal totalCost = numericLines.Max();
                pricePerLitre = numericLines.Min();

                if (pricePerLitre > 0)
                    litres = totalCost / pricePerLitre;
            }
        }

        return (pricePerLitre, litres);
    }

    public Task<byte[]> PreprocessImageForOcrAsync(byte[] rawImageBytes)
        => Task.Run(() => PreprocessImageForOcr(rawImageBytes));

    private static byte[] PreprocessImageForOcr(byte[] rawImageBytes)
    {
        using var original = SKBitmap.Decode(rawImageBytes);
        if (original == null) return rawImageBytes;

        // Resize (keep under 1280–1500px for speed + accuracy)
        int maxDim = 1400;
        float scale = Math.Min((float)maxDim / original.Width, (float)maxDim / original.Height);
        int newW = scale < 1 ? (int)(original.Width * scale) : original.Width;
        int newH = scale < 1 ? (int)(original.Height * scale) : original.Height;

        using var resized = new SKBitmap(new SKImageInfo(newW, newH, SKColorType.Gray8));
        original.ScalePixels(resized, SKFilterQuality.High);

        //Improve contrast & sharpness ===
        using var sharpened = new SKBitmap(resized.Info);
        using var canvas = new SKCanvas(sharpened);

        // High contrast + slight sharpen
        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
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
}