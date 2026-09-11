using NevApps.Classes;
using SwipeDirection = MudBlazor.SwipeDirection;

namespace MobileNevApps.Tests.Classes;

public class AppNavTests
{
    [Fact]
    public void GetDestinations_IncludesHomeAndSettings_WhenTrackersAreOn()
    {
        var destinations = AppNav.GetDestinations(expenseEnabled: true, mileageEnabled: true, shenshaiEnabled: false, stepsEnabled: false);

        Assert.Equal(new[] { AppNav.Home, AppNav.Expense, AppNav.Mileage, AppNav.Settings }, destinations);
    }

    [Fact]
    public void GetDestinations_OmitsHome_WhenOnlyStepsAreOn()
    {
        var destinations = AppNav.GetDestinations(false, false, false, stepsEnabled: true);

        Assert.Equal(new[] { AppNav.Steps, AppNav.Settings }, destinations);
    }

    [Theory]
    [InlineData("expense-tracker/view", AppNav.Expense)]
    [InlineData("/expense-tracker/edit/4", AppNav.Expense)]
    [InlineData("mileage-tracker/view", AppNav.Mileage)]
    [InlineData("/reminders", AppNav.Shenshai)]
    [InlineData("/sunrisesunset", AppNav.Shenshai)]
    [InlineData("settings/db", AppNav.Settings)]
    [InlineData("", AppNav.Home)]
    [InlineData("/", AppNav.Home)]
    public void ResolveDestination_MapsRelatedRoutesToAppBarTargets(string path, string expected)
    {
        Assert.Equal(expected, AppNav.ResolveDestination(path));
    }

    [Fact]
    public void IsActive_TreatsExpenseViewAsExpenseSection()
    {
        Assert.True(AppNav.IsActive("expense-tracker/view", AppNav.Expense));
        Assert.False(AppNav.IsActive("expense-tracker/view", AppNav.Home));
    }

    [Fact]
    public void GetSwipeTarget_RightToLeft_AdvancesToNextSection()
    {
        var destinations = AppNav.GetDestinations(true, true, true, true);

        var target = AppNav.GetSwipeTarget("/", SwipeDirection.RightToLeft, destinations);

        Assert.Equal(AppNav.Expense, target);
    }

    [Fact]
    public void GetSwipeTarget_LeftToRight_GoesToPreviousSection()
    {
        var destinations = AppNav.GetDestinations(true, true, true, true);

        var target = AppNav.GetSwipeTarget("expense-tracker/view", SwipeDirection.LeftToRight, destinations);

        Assert.Equal(AppNav.Home, target);
    }

    [Fact]
    public void GetSwipeTarget_AtEnds_WrapsAround()
    {
        var destinations = AppNav.GetDestinations(true, false, false, false);

        Assert.Equal(AppNav.Settings, AppNav.GetSwipeTarget("/", SwipeDirection.LeftToRight, destinations));
        Assert.Equal(AppNav.Home, AppNav.GetSwipeTarget("/view-settings", SwipeDirection.RightToLeft, destinations));
        Assert.Null(AppNav.GetSwipeTarget("/", SwipeDirection.TopToBottom, destinations));
    }

    [Fact]
    public void GetSwipeTarget_SingleDestination_ReturnsNull()
    {
        var destinations = AppNav.GetDestinations(false, false, false, false);

        Assert.Equal(new[] { AppNav.Settings }, destinations);
        Assert.Null(AppNav.GetSwipeTarget("/view-settings", SwipeDirection.RightToLeft, destinations));
        Assert.Null(AppNav.GetSwipeTarget("/view-settings", SwipeDirection.LeftToRight, destinations));
    }
}
