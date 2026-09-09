using Microsoft.EntityFrameworkCore;
using MobileNevApps.Tests.Helpers;
using NevDBClass.Models;

namespace MobileNevApps.Tests.Data;

public class SqLiteDbContextTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ExpenseUniqueIndex_RejectsDuplicateBusinessKey()
    {
        await using var db = _factory.CreateDbContext();
        db.ExpenseTrackers.Add(AppTestHarness.Expense());
        await db.SaveChangesAsync();

        db.ExpenseTrackers.Add(AppTestHarness.Expense());
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task MileageUniqueIndex_RejectsDuplicateBusinessKey()
    {
        await using var db = _factory.CreateDbContext();
        db.MileageTrackers.Add(AppTestHarness.MileageEntry());
        await db.SaveChangesAsync();

        db.MileageTrackers.Add(AppTestHarness.MileageEntry());
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ReminderUniqueIndex_RejectsDuplicateNameTypeDate()
    {
        await using var db = _factory.CreateDbContext();
        var reminder = new Reminder
        {
            Name = "Dad",
            Type = Reminder.ReminderType.Birthday,
            Date = new DateOnly(2026, 4, 1)
        };
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync();

        db.Reminders.Add(new Reminder
        {
            Name = "Dad",
            Type = Reminder.ReminderType.Birthday,
            Date = new DateOnly(2026, 4, 1)
        });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ReminderType_PersistsAsEnum()
    {
        await using (var db = _factory.CreateDbContext())
        {
            db.Reminders.Add(new Reminder
            {
                Name = "Mom",
                Type = Reminder.ReminderType.Death,
                Date = new DateOnly(2026, 1, 1)
            });
            await db.SaveChangesAsync();
        }

        await using (var db = _factory.CreateDbContext())
        {
            var saved = await db.Reminders.SingleAsync();
            Assert.Equal(Reminder.ReminderType.Death, saved.Type);
        }
    }
}
