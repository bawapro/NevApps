using Bunit;
using MobileNevApps.Tests.Helpers;
using ExpenseAdd = NevApps.Components.Pages.ExpenseTracker.Add;
using ExpenseEdit = NevApps.Components.Pages.ExpenseTracker.Edit;
using ExpenseView = NevApps.Components.Pages.ExpenseTracker.View;

namespace MobileNevApps.Tests.Pages;

public class ExpenseTrackerPageTests : BunitPageContext
{
    [Fact]
    public void Add_RendersFormAndScanAction()
    {
        var cut = Render<ExpenseAdd>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Expense Tracker", cut.Markup);
            Assert.Contains("Add your expense or income details", cut.Markup);
            Assert.Contains("Scan Receipt", cut.Markup);
            Assert.Contains("Search by Category, Type, Place", cut.Markup);
        });
    }

    [Fact]
    public async Task View_ListsSavedExpense()
    {
        await Harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(
            category: "Food", type: "Groceries", place: "Costco", detail: "Weekly shop", price: 45.67m));

        var cut = Render<ExpenseView>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Expense Tracker - View", cut.Markup);
            Assert.Contains("Costco", cut.Markup);
            Assert.Contains("Weekly shop", cut.Markup);
        });
    }

    [Fact]
    public async Task Edit_LoadsExistingExpenseIntoDialog()
    {
        var expense = AppTestHarness.Expense(place: "Starbucks", price: 6.25m, detail: "Latte");
        await Harness.Expenses.AddExpenseAsync(expense);

        var cut = Render<ExpenseEdit>(ps => ps.Add(p => p.Expense, expense));

        Assert.NotNull(cut.Instance);
    }
}
