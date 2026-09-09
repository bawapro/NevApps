using Bunit;
using NevApps.Components.Pages.Settings;
using MobileNevApps.Tests.Helpers;
using SettingsView = NevApps.Components.Pages.Settings.View;

namespace MobileNevApps.Tests.Pages;

public class SettingsPageTests : BunitPageContext
{
    [Fact]
    public void View_ShowsAllSettingsSections()
    {
        var cut = Render<SettingsView>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Settings", cut.Markup);
            Assert.Contains("Apps Selection", cut.Markup);
            Assert.Contains("Country Selection", cut.Markup);
            Assert.Contains("Vehicle Selection", cut.Markup);
            Assert.Contains("Category Selection", cut.Markup);
            Assert.Contains("Backup-Restore-Clear", cut.Markup);
            Assert.Contains("Save Settings", cut.Markup);
        });
    }

    [Fact]
    public void AppSelection_ListsEachModuleToggle()
    {
        var cut = Render<AppSelection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Expense Tracker", cut.Markup);
            Assert.Contains("Mileage Tracker", cut.Markup);
            Assert.Contains("Steps Tracker", cut.Markup);
            Assert.Contains("Shahenshahi Calendar", cut.Markup);
        });
    }

    [Fact]
    public void CountrySelection_ShowsResidenceHelper()
    {
        var cut = Render<CountrySelection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Country of Residence", cut.Markup);
            Assert.Contains("CAD", cut.Markup);
        });
    }

    [Fact]
    public void VehicleSelection_ShowsSavedVehicle()
    {
        var cut = Render<VehicleSelection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Civic", cut.Markup);
            Assert.Contains("Start Location", cut.Markup);
            Assert.Contains("Vehicle Type", cut.Markup);
        });
    }

    [Fact]
    public void CategorySelection_ShowsBuiltInCategories()
    {
        var cut = Render<CategorySelection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Food", cut.Markup);
            Assert.Contains("Car", cut.Markup);
            Assert.Contains("Shopping", cut.Markup);
        });
    }

    [Fact]
    public void Database_ShowsBackupActions()
    {
        var cut = Render<DB>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Backup", cut.Markup);
        });
    }
}
