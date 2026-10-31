using Microsoft.EntityFrameworkCore;
using NevApps.Classes;
using NevDBClass.Data;
using NevDBClass.Models;

namespace NevApps.Services;

public class ReminderService
{
    private readonly IDbContextFactory<SqLiteDbContext> _db;
    private readonly LoggerService _loggerService;

    public ReminderService(IDbContextFactory<SqLiteDbContext> db, LoggerService loggerService)
    {
        _db = db;
        _loggerService = loggerService;
    }

    /// <summary>
    ///     Retrieves all reminders ordered by date.
    /// </summary>
    public async Task<List<Reminder>> GetAllRemindersAsync()
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            return await db.Reminders
                .OrderBy(r => r.Name)
                .ThenBy(r => r.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetAllRemindersAsync", ex);
            throw;
        }
    }

    public async Task<bool> HasUpcomingRemindersAsync(int days = 7)
    {
        var reminders = await GetAllRemindersAsync();
        var calendar = new ShenshahiCalendar();
        var today = DateTime.Today;
        var until = today.AddDays(days);
        var year = today.Year;

        return reminders.Any(item =>
        {
            var date = calendar.GetDateFromRojMah(item.Mah, item.Roj, year);
            if (date.Date < today)
                date = calendar.GetDateFromRojMah(item.Mah, item.Roj, year + 1);
            return date.Date >= today && date.Date <= until;
        });
    }

    /// <summary>
    ///     Get Reminders from x days.
    /// </summary>
    public async Task<List<Reminder>> GetRemindersAsPerShenshaiAsync(int days)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            var targetDate = DateOnly.FromDateTime(DateTime.Now.AddDays(days));
            return await db.Reminders
                .Where(x => x.Date > targetDate)
                .OrderBy(r => r.Date)
                .ThenBy(r => r.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in GetRemindersAsync", ex);
            throw;
        }
    }

    /// <summary>
    ///     Adds a new reminder record.
    /// </summary>
    public async Task<int> AddReminderAsync(Reminder reminder)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            await db.Reminders.AddAsync(reminder);
            return await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in AddReminderAsync", ex, $"Name: {reminder.Name}");
            throw;
        }
    }

    /// <summary>
    ///     Updates an existing reminder record.
    /// </summary>
    public async Task<int> UpdateReminderAsync(Reminder reminder)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            db.Reminders.Update(reminder);
            return await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in UpdateReminderAsync", ex, $"Id: {reminder.Id}");
            throw;
        }
    }

    /// <summary>
    ///     Deletes a reminder record by ID.
    /// </summary>
    public async Task<int> DeleteReminderAsync(int id)
    {
        try
        {
            using var db = await _db.CreateDbContextAsync();
            var reminder = await db.Reminders.FindAsync(id);
            if (reminder != null)
            {
                db.Reminders.Remove(reminder);
                return await db.SaveChangesAsync();
            }

            return 0;
        }
        catch (Exception ex)
        {
            _loggerService.Log("Error in DeleteReminderAsync", ex, $"Id: {id}");
            throw;
        }
    }
}