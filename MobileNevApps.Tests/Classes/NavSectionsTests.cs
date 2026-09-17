using NevApps.Classes;

namespace MobileNevApps.Tests.Classes;

public class NavSectionsTests
{
    private static readonly NavSections.Features AllOn = new(true, true, true, true, false);
    private static readonly NavSections.Features ExpenseOnly = new(true, false, false, false, false);
    private static readonly NavSections.Features StepsOnly = new(false, false, false, true, false);

    [Fact]
    public void VisiblePages_IncludesHomeAndSettings_WhenTrackersAreOn()
    {
        var pages = NavSections.VisiblePages(new(true, true, false, false, false));

        Assert.Equal(new[] { NavSections.Home, NavSections.Expense, NavSections.Mileage, NavSections.Settings }, pages);
    }

    [Fact]
    public void VisiblePages_OmitsHome_WhenOnlyStepsAreOn()
    {
        var pages = NavSections.VisiblePages(StepsOnly, NavSections.Steps);

        Assert.Equal(new[] { NavSections.Steps, NavSections.Settings }, pages);
    }

    [Fact]
    public void IsShellRoute_OnlyMatchesCanonicalAddAndViewRoots()
    {
        Assert.True(NavSections.IsShellRoute("/"));
        Assert.True(NavSections.IsShellRoute("expense-tracker/add"));
        Assert.True(NavSections.IsShellRoute("/view-settings?x=1"));
        Assert.False(NavSections.IsShellRoute("/expense-tracker/view"));
        Assert.False(NavSections.IsShellRoute("/mileage-tracker/edit/4"));
        Assert.False(NavSections.IsShellRoute("/reminders"));
    }

    [Fact]
    public void Neighbor_WrapsAtBothEnds()
    {
        Assert.Equal(NavSections.Expense, NavSections.Neighbor("/", NavSections.Next, AllOn));
        Assert.Equal(NavSections.Settings, NavSections.Neighbor("/", NavSections.Previous, AllOn));
        Assert.Equal(NavSections.Home, NavSections.Neighbor("/view-settings", NavSections.Next, AllOn));
        Assert.Equal(NavSections.Settings, NavSections.Neighbor("/", NavSections.Next, new(false, false, false, false, false)));
    }

    [Fact]
    public void Neighbor_SkipsDisabledSections()
    {
        Assert.Equal(NavSections.Expense, NavSections.Neighbor("/", NavSections.Next, ExpenseOnly));
        Assert.Equal(NavSections.Settings, NavSections.Neighbor(NavSections.Expense, NavSections.Next, ExpenseOnly));
        Assert.Equal(NavSections.Expense, NavSections.Neighbor("/view-settings", NavSections.Previous, ExpenseOnly));
        Assert.Equal(NavSections.Home, NavSections.Neighbor("/view-settings", NavSections.Next, ExpenseOnly));
    }

    [Fact]
    public void Slot_WrapsNeighborsAndTwoPageDrag()
    {
        Assert.Equal(0, NavSections.Slot(1, 1, 5));
        Assert.Equal(1, NavSections.Slot(2, 1, 5));
        Assert.Equal(-1, NavSections.Slot(0, 1, 5));
        Assert.Equal(1, NavSections.Slot(1, 0, 2, dragX: 0));
        Assert.Equal(-1, NavSections.Slot(1, 0, 2, dragX: 10));
    }
}
