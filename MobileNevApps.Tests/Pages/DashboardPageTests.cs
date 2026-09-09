using Bunit;
using NevApps.Components.Pages.Dashboard;
using MobileNevApps.Tests.Helpers;

namespace MobileNevApps.Tests.Pages;

public class DashboardPageTests : BunitPageContext
{
    [Fact]
    public async Task ExpenseSummary_ShowsCategoryTotal()
    {
        await Harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(
            category: "Food", place: "Costco", price: 45.67m));

        var cut = Render<ExpenseSummary>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Food", cut.Markup);
            Assert.Contains("Tap for details", cut.Markup);
        });
    }

    [Fact]
    public async Task AutoPay_ShowsUpcomingPaymentsHeadingWhenConfigured()
    {
        await Harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(
            place: "Rent",
            category: "Bank",
            type: "Bill",
            detail: "Apartment",
            price: 1200m,
            autoPay: 1,
            autoPayDate: DateOnly.FromDateTime(DateTime.Today),
            frequency: "M"));

        var cut = Render<AutoPay>();

        cut.WaitForAssertion(() => Assert.Contains("Your Upcoming Payments", cut.Markup));
    }

    [Fact]
    public async Task MileageSummary_ShowsVehicleWhenTripsExist()
    {
        await Harness.Mileage.AddMileageAsync(AppTestHarness.MileageEntry(vehicle: "Civic"));

        var cut = Render<MileageSummary>();

        cut.WaitForAssertion(() => Assert.Contains("Civic", cut.Markup));
    }

    [Fact]
    public async Task RemindersWidget_ShowsUpcomingHeading()
    {
        var calendar = new NevApps.Classes.ShenshahiCalendar();
        var (roj, mah, _) = calendar.GetRojAndMah(DateTime.Today);
        await Harness.Reminders.AddReminderAsync(new NevDBClass.Models.Reminder
        {
            Name = "Today Event",
            Type = NevDBClass.Models.Reminder.ReminderType.Birthday,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Mah = mah,
            Roj = roj
        });

        var cut = Render<Reminders>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Upcoming Reminders", cut.Markup);
            Assert.Contains("Today Event", cut.Markup);
        });
    }
}
