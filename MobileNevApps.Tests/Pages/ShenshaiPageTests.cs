using Bunit;
using NevApps.Components.Pages.Shenshai;
using MobileNevApps.Tests.Helpers;
using ShenshaiView = NevApps.Components.Pages.Shenshai.View;

namespace MobileNevApps.Tests.Pages;

public class ShenshaiPageTests : BunitPageContext
{
    [Fact]
    public void Calendar_ShowsTitleAndDatePickers()
    {
        var cut = Render<ShenshaiView>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Shahenshahi Calendar", cut.Markup);
            Assert.Contains("Y.Z.", cut.Markup);
        });
    }

    [Fact]
    public void Reminders_ShowsRojMahHelpAndAddForm()
    {
        var cut = Render<Reminders>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Manage Reminders", cut.Markup);
            Assert.Contains("Roj-Mah", cut.Markup);
            Assert.Contains("Add New Reminder", cut.Markup);
        });
    }

    [Fact]
    public async Task Reminders_ListsSavedReminder()
    {
        await Harness.Reminders.AddReminderAsync(new NevDBClass.Models.Reminder
        {
            Name = "Navroz",
            Type = NevDBClass.Models.Reminder.ReminderType.Anniversary,
            Date = new DateOnly(2026, 8, 16),
            Mah = "Fravardin",
            Roj = "Hormazd"
        });

        var cut = Render<Reminders>();

        cut.WaitForAssertion(() => Assert.Contains("Navroz", cut.Markup));
    }
}
