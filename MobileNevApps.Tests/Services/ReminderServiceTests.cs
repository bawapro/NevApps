using MobileNevApps.Tests.Helpers;
using NevDBClass.Models;

namespace MobileNevApps.Tests.Services;

public class ReminderServiceTests : IDisposable
{
    private readonly AppTestHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task AddUpdateDeleteReminder_PersistsChanges()
    {
        var reminder = new Reminder
        {
            Name = "Navroz",
            Type = Reminder.ReminderType.Anniversary,
            Date = new DateOnly(2026, 8, 15),
            Mah = "Fravardin",
            Roj = "Hormazd",
            Notes = "New year"
        };

        Assert.Equal(1, await _harness.Reminders.AddReminderAsync(reminder));
        var saved = (await _harness.Reminders.GetAllRemindersAsync()).Single();
        Assert.Equal("Navroz", saved.Name);

        saved.Notes = "Updated";
        Assert.Equal(1, await _harness.Reminders.UpdateReminderAsync(saved));
        Assert.Equal("Updated", (await _harness.Reminders.GetAllRemindersAsync()).Single().Notes);

        Assert.Equal(1, await _harness.Reminders.DeleteReminderAsync(saved.Id));
        Assert.Empty(await _harness.Reminders.GetAllRemindersAsync());
        Assert.Equal(0, await _harness.Reminders.DeleteReminderAsync(saved.Id));
    }

    [Fact]
    public async Task GetAllRemindersAsync_OrdersByNameThenDate()
    {
        await _harness.Reminders.AddReminderAsync(new Reminder
        {
            Name = "Zara",
            Type = Reminder.ReminderType.Birthday,
            Date = new DateOnly(2026, 1, 2)
        });
        await _harness.Reminders.AddReminderAsync(new Reminder
        {
            Name = "Ava",
            Type = Reminder.ReminderType.Birthday,
            Date = new DateOnly(2026, 5, 1)
        });

        var all = await _harness.Reminders.GetAllRemindersAsync();
        Assert.Equal(new[] { "Ava", "Zara" }, all.Select(r => r.Name));
    }

}
