using NevApps.Classes;

namespace MobileNevApps.Tests.Classes;

public class ConstantsTests
{
    [Fact]
    public void AutoPay_Frequencies_ContainAllExpectedCodes()
    {
        Assert.Equal("Daily", Constants.AutoPay.Frequencies[Constants.AutoPay.Daily]);
        Assert.Equal("Weekly", Constants.AutoPay.Frequencies[Constants.AutoPay.Weekly]);
        Assert.Equal("Bi-weekly", Constants.AutoPay.Frequencies[Constants.AutoPay.BiWeekly]);
        Assert.Equal("Monthly", Constants.AutoPay.Frequencies[Constants.AutoPay.Monthly]);
        Assert.Equal("Quarterly", Constants.AutoPay.Frequencies[Constants.AutoPay.Quarterly]);
        Assert.Equal("Half-yearly", Constants.AutoPay.Frequencies[Constants.AutoPay.HalfYearly]);
        Assert.Equal("Yearly", Constants.AutoPay.Frequencies[Constants.AutoPay.Yearly]);
        Assert.Equal(7, Constants.AutoPay.Frequencies.Count);
    }

    [Fact]
    public void Conversions_MetricAndImperial_AreInverses()
    {
        Assert.Equal(1m, Constants.Conversions.KmToMiles * Constants.Conversions.MilesToKm, 5);
        Assert.Equal(1m, Constants.Conversions.LitresToUsGallons * Constants.Conversions.UsGallonsToLitres, 5);
    }

    [Fact]
    public void Categories_IncludeTheBuiltInSet()
    {
        Assert.Contains(Constants.Categories.Food, Constants.Categories.AllCategories);
        Assert.Contains(Constants.Categories.Car, Constants.Categories.AllCategories);
        Assert.Equal(7, Constants.Categories.AllCategories.Count);
    }

    [Theory]
    [InlineData("tesla")]
    [InlineData("leaf")]
    [InlineData("mach-e")]
    [InlineData("ioniq")]
    public void EvIndicators_ContainKnownElectricVehicles(string indicator)
    {
        Assert.Contains(indicator, Constants.EvIndicators);
    }

    [Fact]
    public void DatabaseConstants_HaveExpectedValues()
    {
        Assert.Equal("NevDB.db3", Constants.DbName);
        Assert.Equal("backups", Constants.BackupFolderName);
        Assert.Equal(5, Constants.MaxBackups);
        Assert.False(string.IsNullOrWhiteSpace(Constants.OnLoadErrorMessage));
    }
}
