using Bunit;
using MobileNevApps.Tests.Helpers;
using MileageAdd = NevApps.Components.Pages.MileageTracker.Add;
using MileageEdit = NevApps.Components.Pages.MileageTracker.Edit;
using MileageView = NevApps.Components.Pages.MileageTracker.View;

namespace MobileNevApps.Tests.Pages;

public class MileageTrackerPageTests : BunitPageContext
{
    [Fact]
    public void Add_RendersTravelFormAndScanAction()
    {
        var cut = Render<MileageAdd>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Mileage Tracker", cut.Markup);
            Assert.Contains("Add your travel and fuel details", cut.Markup);
            Assert.Contains("Scan Receipt", cut.Markup);
        });
    }

    [Fact]
    public async Task View_ListsSavedTrip()
    {
        await Harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(
            start: "Home", end: "Work * Downtown", detail: "Commute"));

        var cut = Render<MileageView>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Mileage Tracker - View", cut.Markup);
            Assert.Contains("Home", cut.Markup);
            Assert.Contains("Commute", cut.Markup);
        });
    }

    [Fact]
    public async Task Edit_LoadsExistingTripIntoDialog()
    {
        var trip = AppTestHarness.MileageEntry(start: "Garage", end: "Airport * YYZ");
        await Harness.Mileage.AddMileageAsync(trip);

        var cut = Render<MileageEdit>(ps => ps.Add(p => p.MileageData, trip));

        Assert.NotNull(cut.Instance);
    }
}
