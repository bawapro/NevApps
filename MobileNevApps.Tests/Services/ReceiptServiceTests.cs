using NevApps.Services;

namespace MobileNevApps.Tests.Services;

public class ReceiptServiceTests
{
    private readonly ReceiptService _service = new();

    private static string Pad(params string[] lines)
    {
        var list = lines.ToList();
        while (list.Count < 12)
            list.Add($"line{list.Count}");
        return string.Join('\n', list);
    }

    [Fact]
    public void ParseReceiptData_ReadsPlaceAndTotal()
    {
        var raw = Pad(
            "COSTCO WHOLESALE",
            "123 Main Street",
            "Toronto ON",
            "Date 03/15/2025 10:30 AM",
            "Milk 4.99",
            "Bread 3.50",
            "TOTAL 45.67",
            "Thank you");

        var (date, place, price) = _service.ParseReceiptData(raw, new List<string> { "COSTCO WHOLESALE", "Walmart" });

        Assert.Equal("COSTCO WHOLESALE", place);
        Assert.Equal(45.67m, price);
        Assert.Equal(2025, date.Year);
        Assert.Equal(3, date.Month);
        Assert.Equal(15, date.Day);
    }

    [Fact]
    public void ParseReceiptData_UnknownMerchant_ReturnsUnknownPlace()
    {
        var raw = Pad(
            "RANDOM MART",
            "Somewhere",
            "Date 01/02/2025 09:00 AM",
            "TOTAL 9.99");

        var (_, place, price) = _service.ParseReceiptData(raw, new List<string> { "Costco" });
        Assert.Equal("Unknown", place);
        Assert.Equal(9.99m, price);
    }

    [Fact]
    public void ParseReceiptData_CreditCardSlip_UsesMidAidRef()
    {
        var raw = Pad(
            "SHELL CANADA",
            "MID 12345",
            "AID A00000",
            "REF 999",
            "03/21/2025",
            "AMOUNT 21.00");

        var (date, place, price) = _service.ParseReceiptData(raw, new List<string> { "SHELL CANADA" });
        Assert.Equal("SHELL CANADA", place);
        Assert.Equal(new DateTime(2025, 3, 21), date.Date);
        Assert.Equal(21.00m, price);
    }

    [Fact]
    public void ParseGasReceiptData_ReadsStationRateAndLitres()
    {
        var raw = string.Join('\n', new[]
        {
            "PETRO-CANADA",
            "100 King Street",
            "DATE 2025-03-15",
            "40.000 L AT $1.459 /L",
            "TOTAL $58.36"
        });

        var (date, station, address, rate, filled) =
            _service.ParseGasReceiptData(raw, new List<string> { "PETRO-CANADA", "Shell" });

        Assert.Equal("PETRO-CANADA", station);
        Assert.Equal("100 King Street", address);
        Assert.Equal(1.459m, rate);
        Assert.Equal(40.000m, filled);
        Assert.Equal(2025, date.Year);
    }

    [Fact]
    public void ParseGasReceiptData_UnknownStation_LeavesAddressEmpty()
    {
        var raw = string.Join('\n', new[]
        {
            "UNKNOWN FUEL",
            "Somewhere",
            "DATE 2025-03-15",
            "10.5 L AT $1.20 /L"
        });

        var (_, station, address, rate, filled) =
            _service.ParseGasReceiptData(raw, new List<string> { "Petro" });

        Assert.Equal("Unknown", station);
        Assert.Equal(string.Empty, address);
        Assert.Equal(1.20m, rate);
        Assert.Equal(10.5m, filled);
    }
}
