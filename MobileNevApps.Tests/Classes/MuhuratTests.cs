using NevApps.Classes;

namespace MobileNevApps.Tests.Classes;

public class MuhuratTests
{
    [Theory]
    [InlineData(2026, 9, 6, 7, 0, "Udveg")]   // Sunday day first slot
    [InlineData(2026, 9, 6, 8, 10, "Chal")]   // Sunday inclusive start of next slot
    [InlineData(2026, 9, 6, 17, 10, "Udveg")] // Sunday 17:10 starts last day slot
    [InlineData(2026, 9, 6, 19, 0, "Shub")]   // Sunday night
    [InlineData(2026, 9, 7, 7, 0, "Amrit")]   // Monday day
    [InlineData(2026, 9, 7, 0, 30, "Labh")]   // Monday 00:30 is Monday night wrap
    public void GetCurrentMuhurat_ReturnsExpectedName(int year, int month, int day, int hour, int minute, string expected)
    {
        var when = new DateTime(year, month, day, hour, minute, 0);
        Assert.Equal(expected, Muhurat.GetCurrentMuhurat(when));
    }

    [Fact]
    public void GetFullDayMuhurat_Sunday_HasSixteenSlots()
    {
        var sunday = new DateTime(2026, 9, 6);
        var timeline = Muhurat.GetFullDayMuhurat(sunday);

        Assert.Equal(16, timeline.Count);
        Assert.Equal("Udveg", timeline[0].Name);
        Assert.Equal("06:40 - 08:10", timeline[0].TimeRange);
        Assert.Equal("Shub", timeline[^1].Name);
    }

    [Fact]
    public void GetFullDayMuhurat_EachWeekday_HasDayAndNightSlots()
    {
        var monday = new DateTime(2026, 9, 7);
        for (int i = 0; i < 7; i++)
        {
            var timeline = Muhurat.GetFullDayMuhurat(monday.AddDays(i));
            Assert.Equal(16, timeline.Count);
        }
    }
}
