using Bunit;
using NevApps.Components.Pages;
using MobileNevApps.Tests.Helpers;

namespace MobileNevApps.Tests.Pages;

public class HomePageTests : BunitPageContext
{
    [Fact]
    public void Home_FirstLaunch_ShowsWelcomeAndSettingsLink()
    {
        SeedFirstLaunch();

        var cut = Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Welcome to NevApps", cut.Markup);
            Assert.Contains("set up your profile", cut.Markup);
            Assert.Contains("/view-settings", cut.Markup);
        });
    }

    [Fact]
    public void Home_ConfiguredButEmpty_ShowsStartAddingMessage()
    {
        using (var db = Harness.Db.CreateDbContext())
        {
            db.Settings.RemoveRange(db.Settings);
            db.SaveChanges();
            db.Settings.AddRange(
                AppTestHarness.Setting("country", "Canada"),
                AppTestHarness.Setting("currency", "CAD"),
                AppTestHarness.Setting("expensetracker", "1"));
            db.SaveChanges();
        }

        var cut = Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Start adding expenses, mileage, and auto payment details", cut.Markup);
            Assert.DoesNotContain("Welcome to NevApps", cut.Markup);
        });
    }

    [Fact]
    public async Task Home_WithExpenseData_ShowsExpensesTab()
    {
        await Harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(autoPay: 2, place: "Costco", price: 45.67m));

        var cut = Render<Home>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Expenses", cut.Markup);
            Assert.Contains("Food", cut.Markup);
        });
    }
}
