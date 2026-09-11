using Bunit;
using Microsoft.AspNetCore.Components;
using MobileNevApps.Tests.Helpers;
using MudBlazor;
using NevApps.Classes;
using SwipeDirection = MudBlazor.SwipeDirection;
using PointerEventArgs = Microsoft.AspNetCore.Components.Web.PointerEventArgs;
using NevApps.Components.Layout;
using NevApps.Components.Pages;

namespace MobileNevApps.Tests.Pages;

public class SwipeNavigationTests : BunitPageContext
{
    [Fact]
    public async Task SwipeNavigator_RightToLeft_NavigatesToNextAppSection()
    {
        var nav = Ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo(AppNav.Home);

        var cut = Render<SwipeNavigator>(ps => ps
            .Add(p => p.ExpenseEnabled, true)
            .Add(p => p.MileageEnabled, true)
            .Add(p => p.ShenshaiEnabled, true)
            .Add(p => p.StepsEnabled, true)
            .AddChildContent("<p>body</p>"));

        await InvokeSwipe(cut, SwipeDirection.RightToLeft);

        Assert.EndsWith(AppNav.Expense, nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SwipeNavigator_LeftToRight_NavigatesToPreviousAppSection()
    {
        var nav = Ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo(AppNav.Mileage);

        var cut = Render<SwipeNavigator>(ps => ps
            .Add(p => p.ExpenseEnabled, true)
            .Add(p => p.MileageEnabled, true)
            .Add(p => p.ShenshaiEnabled, false)
            .Add(p => p.StepsEnabled, false)
            .AddChildContent("<p>body</p>"));

        await InvokeSwipe(cut, SwipeDirection.LeftToRight);

        Assert.EndsWith(AppNav.Expense, nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SwipeNavigator_WrapsFromLastSectionToFirst()
    {
        var nav = Ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo(AppNav.Settings);

        var cut = Render<SwipeNavigator>(ps => ps
            .Add(p => p.ExpenseEnabled, true)
            .AddChildContent("<p>body</p>"));

        await InvokeSwipe(cut, SwipeDirection.RightToLeft);

        Assert.EndsWith(AppNav.Home, nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SwipeNavigator_WrapsFromFirstSectionToLast()
    {
        var nav = Ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo(AppNav.Home);

        var cut = Render<SwipeNavigator>(ps => ps
            .Add(p => p.ExpenseEnabled, true)
            .AddChildContent("<p>body</p>"));

        await InvokeSwipe(cut, SwipeDirection.LeftToRight);

        Assert.EndsWith(AppNav.Settings, nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SwipeNavigator_OnSwipeMove_NavigatesOnceThresholdIsCrossed()
    {
        var nav = Ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo(AppNav.Home);

        var cut = Render<SwipeNavigator>(ps => ps
            .Add(p => p.ExpenseEnabled, true)
            .Add(p => p.MileageEnabled, true)
            .AddChildContent("<p>body</p>"));

        var swipe = cut.FindComponent<MudSwipeArea>();
        var deltas = (IReadOnlyList<double?>)new double?[] { 60, 5 };
        var directions = (IReadOnlyList<SwipeDirection>)new[] { SwipeDirection.RightToLeft, SwipeDirection.None };
        var args = new MultiDimensionSwipeEventArgs(new PointerEventArgs(), directions, deltas, swipe.Instance);
        await cut.InvokeAsync(() => swipe.Instance.OnSwipeMove.InvokeAsync(args));

        Assert.EndsWith(AppNav.Expense, nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Home_SwipeAdvancesThroughDashboardTabsThenYields()
    {
        await Harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(autoPay: 2, place: "Costco", price: 45.67m));
        await Harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry());

        var cut = Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Expenses", cut.Markup);
            Assert.Contains("Mileage", cut.Markup);
        });

        Assert.True(await HandleSwipe(cut, SwipeDirection.RightToLeft));
        cut.WaitForAssertion(() => Assert.Contains("Civic", cut.Markup));

        Assert.True(await HandleSwipe(cut, SwipeDirection.RightToLeft));
        cut.WaitForAssertion(() => Assert.Contains("Upcoming Reminders", cut.Markup));

        Assert.False(await HandleSwipe(cut, SwipeDirection.RightToLeft));
        Assert.True(await HandleSwipe(cut, SwipeDirection.LeftToRight));
        cut.WaitForAssertion(() => Assert.Contains("Civic", cut.Markup));
    }

    [Fact]
    public void MainLayout_WrapsBodyInMudSwipeArea()
    {
        var cut = Render<MainLayout>(ps => ps.Add(p => p.Body, (RenderFragment)(builder =>
            builder.AddMarkupContent(0, "<p>layout-body</p>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindComponent<MudSwipeArea>());
            Assert.Contains("layout-body", cut.Markup);
        });
    }

    private static async Task InvokeSwipe(IRenderedComponent<SwipeNavigator> cut, SwipeDirection direction)
    {
        var swipe = cut.FindComponent<MudSwipeArea>();
        var args = new SwipeEventArgs(new PointerEventArgs(), direction, 140, swipe.Instance);
        await cut.InvokeAsync(() => swipe.Instance.OnSwipeEnd.InvokeAsync(args));
    }

    private static async Task<bool> HandleSwipe(IRenderedComponent<Home> cut, SwipeDirection direction)
    {
        var handled = false;
        await cut.InvokeAsync(() => handled = cut.Instance.TryHandleSwipe(direction));
        return handled;
    }
}
