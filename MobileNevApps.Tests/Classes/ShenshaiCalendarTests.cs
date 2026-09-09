using NevApps.Classes;

namespace MobileNevApps.Tests.Classes;

public class ShenshaiCalendarTests
{
    private readonly ShenshahiCalendar _calendar = new();

    [Fact]
    public void BaseDate_IsSeptember10_1920()
    {
        Assert.Equal(new DateTime(1920, 9, 10), _calendar.BaseDate);
    }

    [Fact]
    public void GetMahAndRojNames_HaveExpectedCounts()
    {
        Assert.Equal(12, _calendar.GetMahNames().Count);
        Assert.Equal(35, _calendar.GetRojNames().Count);
        Assert.Equal("Fravardin", _calendar.GetMahNames()[0]);
        Assert.Equal("Aspandard", _calendar.GetMahNames()[11]);
        Assert.Equal("Hormazd", _calendar.GetRojNames()[0]);
        Assert.Equal("Vahishto-ishti", _calendar.GetRojNames()[34]);
    }

    [Fact]
    public void GetRojAndMah_OnBaseDate_IsHormazdFravardin()
    {
        var (roj, mah, isGatha) = _calendar.GetRojAndMah(_calendar.BaseDate);

        Assert.Equal("Hormazd", roj);
        Assert.Equal("Fravardin", mah);
        Assert.False(isGatha);
    }

    [Fact]
    public void GetYazdegerdiYear_OnBaseDate_Is1290()
    {
        Assert.Equal(1290, _calendar.GetYazdegerdiYear(_calendar.BaseDate));
    }

    [Fact]
    public void GetYazdegerdiYear_AdvancesEvery365Days()
    {
        Assert.Equal(1291, _calendar.GetYazdegerdiYear(_calendar.BaseDate.AddDays(365)));
        Assert.Equal(1292, _calendar.GetYazdegerdiYear(_calendar.BaseDate.AddDays(730)));
    }

    [Fact]
    public void GetRojAndMah_GathaDays_UseAspandardAndGathaRoj()
    {
        var gathaDate = _calendar.BaseDate.AddDays(360);
        var (roj, mah, isGatha) = _calendar.GetRojAndMah(gathaDate);

        Assert.True(isGatha);
        Assert.Equal("Aspandard", mah);
        Assert.Equal("Ahuna-vaiti", roj);
    }

    [Fact]
    public void GetDateFromRojMah_RoundTripsWithGetRojAndMah()
    {
        var target = new DateTime(2026, 3, 21);
        var (roj, mah, _) = _calendar.GetRojAndMah(target);
        var reconstructed = _calendar.GetDateFromRojMah(mah, roj, target.Year);

        var again = _calendar.GetRojAndMah(reconstructed);
        Assert.Equal(roj, again.Roj);
        Assert.Equal(mah, again.Mah);
        Assert.Equal(target.Year, reconstructed.Year);
    }

    [Fact]
    public void GetDateFromRojMah_UnknownMah_Throws()
    {
        Assert.Throws<ArgumentException>(() => _calendar.GetDateFromRojMah("NotAMah", "Hormazd", 2026));
    }

    [Fact]
    public void GetDateFromRojMah_GathaRojOutsideAspandard_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _calendar.GetDateFromRojMah("Fravardin", "Ahuna-vaiti", 2026));
        Assert.Contains("Gatha", ex.Message);
    }

    [Fact]
    public void GetMonthFromMah_AndGetDayFromRoj_ReturnIndexes()
    {
        Assert.Equal(0, _calendar.GetMonthFromMah("Fravardin"));
        Assert.Equal(11, _calendar.GetMonthFromMah("Aspandard"));
        Assert.Equal(-1, _calendar.GetMonthFromMah("Unknown"));
        Assert.Equal(0, _calendar.GetDayFromRoj("Hormazd"));
        Assert.Equal(30, _calendar.GetDayFromRoj("Ahuna-vaiti"));
    }

    [Fact]
    public void GetDaysAndMonths_MatchGregorianNames()
    {
        Assert.Equal(7, _calendar.GetDays().Count);
        Assert.Equal("Sunday", _calendar.GetDays()[0]);
        Assert.Equal(12, _calendar.GetMonths().Count);
        Assert.Equal("January", _calendar.GetMonths()[0]);
    }
}
