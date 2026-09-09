using Bunit;
using NevApps.Components.Pages.StepTracker;
using MobileNevApps.Tests.Helpers;

namespace MobileNevApps.Tests.Pages;

public class StepTrackerPageTests : BunitPageContext
{
    [Fact]
    public void Add_ShowsPermissionStatusStrip()
    {
        var cut = Render<Add>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Location:", cut.Markup);
            Assert.Contains("WHILE USING", cut.Markup);
        });
    }
}
