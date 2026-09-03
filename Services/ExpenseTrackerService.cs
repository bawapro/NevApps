using Microsoft.EntityFrameworkCore;
using NevApps.Classes;
using NevDBClass.Data;
using NevDBClass.Models;
using System.Text.Json;

namespace NevApps.Services
{
    /// <summary>
    /// Service for managing data operations for expenses, income, and auto-payments.
    /// Handles CRUD operations, search filters and trip associations
    /// </summary>
    internal class ExpenseTrackerService
    {
        private readonly IDbContextFactory<SqLiteDbContext> _db;
        private readonly LoggerService _loggerService;

        public ExpenseTrackerService(IDbContextFactory<SqLiteDbContext> dbFactory, LoggerService loggerService)
        {
            _db = dbFactory;
            _loggerService = loggerService;
        }

        /// <summary>
        /// Searches expenses across Category, Type, Place, and Details. 
        /// Excludes auto-payments from results if the search string is empty.
        /// </summary>
        /// <param name="searchString">The keyword to filter by.</param>
        /// <returns>A list of the top 10 matching expense records.</returns>
        public async Task<List<ExpenseTracker>> SearchExpenseAsync(string searchString)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();

                if (string.IsNullOrWhiteSpace(searchString))
                {
                    return await db.ExpenseTrackers
                        .Where(e => e.AutoPay != 1) // Exclude auto payments from default listing
                        .OrderByDescending(e => e.Date)
                        .ThenByDescending(e => e.Id)
                        .Take(10)
                        .ToListAsync();
                }

                var searchPattern = $"%{searchString}%";
                return await db.ExpenseTrackers
                    .OrderByDescending(e => e.Date)
                    .ThenByDescending(e => e.Id)
                    .Where(e => (EF.Functions.Like(e.Category, searchPattern)) ||
                                (EF.Functions.Like(e.Type, searchPattern)) ||
                                (EF.Functions.Like(e.Place, searchPattern)) ||
                                (EF.Functions.Like(e.Detail, searchPattern))
                    )
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Log("Error in SearchExpenseAsync", ex, $"SearchString: {searchString}");
                throw;
            }
        }

        /// <summary>
        /// Adds a new expense or income record to the database.
        /// </summary>
        public async Task<int> AddExpenseAsync(ExpenseTracker obj)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();
                db.ExpenseTrackers.Add(obj);
                return await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Log("Error in AddExpenseAsync", ex, JsonSerializer.Serialize(obj));
                throw;
            }
        }

        /// <summary>
        /// Retrieves all expenses ordered by date (most recent first).
        /// </summary>
        public async Task<List<ExpenseTracker>> GetAllExpensesAsync()
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();
                return await db.ExpenseTrackers
                    .AsNoTracking()
                    .OrderByDescending(e => e.Date)
                    .ThenByDescending(e => e.Id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Log("Error in GetAllExpensesAsync", ex);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing expense record.
        /// </summary>
        public async Task<int> UpdateExpenseAsync(ExpenseTracker obj)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();
                db.ExpenseTrackers.Update(obj);
                return await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Log("Error in UpdateExpenseAsync", ex, JsonSerializer.Serialize(obj));
                throw;
            }
        }

        /// <summary>
        /// Removes an expense record by ID.
        /// </summary>
        public async Task<int> DeleteExpenseAsync(int id)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();
                var expense = await db.ExpenseTrackers.FindAsync(id);
                if (expense != null)
                {
                    db.ExpenseTrackers.Remove(expense);
                    return await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _loggerService.Log("Error in DeleteExpenseAsync", ex, $"Id: {id}");
                throw;
            }

            return 0;
        }

        /// <summary>
        /// Links an expense record to a specific vehicle trip.
        /// </summary>
        public async Task<ExpenseTracker?> GetExpenseObjByTripIdAsync(int tripId)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();
                return await db.ExpenseTrackers.AsNoTracking().Where(e => e.TripId == tripId).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Log($"Error in GetExpenseObjByTripIdAsync", ex, $"TripId: {tripId}");
                throw;
            }
        }

        public async Task<(string Category, string Type, string Detail)> GetDataByPlaceAsync(string place)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();

                var expense = await db.ExpenseTrackers
                    .AsNoTracking()
                    .Where(e => e.Place == place)
                    .OrderByDescending(o => o.Date)
                    .FirstOrDefaultAsync();

                return (
                    expense.Category ?? "",
                    expense.Type ?? "",
                    expense.Detail ?? "");
            }
            catch (Exception ex)
            {
                _loggerService.Log($"Error in GetExpenseCategoryByPlaceAsync", ex, $"Place: {place}");
                return ("Shopping", "Store", place);
            }
        }


        /// <summary>
        /// Retrieves unique categories. If fromDB is false, it pulls pre-defined categories from Settings.
        /// </summary>
        public async Task<List<string>> GetCategoriesAsync(string? value, bool fromDB)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();

                if (fromDB == false)
                {
                    return (await db.Settings.AsNoTracking()
                            .Where(c => c.SettingKey == "category") // exact match, not Contains
                            .Select(c => c.SettingValue)
                            .FirstOrDefaultAsync() ?? "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(c => c)
                        .ToList();
                }

                IQueryable<ExpenseTracker> query = db.ExpenseTrackers.AsNoTracking();

                if (!string.IsNullOrEmpty(value))
                {
                    query = query.Where(e => e.Category.ToLower().Contains(value.ToLower()));
                }

                return await query
                    .Select(e => e.Category)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Log($"Error in GetCategoriesAsync", ex, $"Value: {value}");
                throw;
            }
        }

        /// <summary>
        /// Gets distinct expense types (for a category if passed) for UI autocomplete.
        /// </summary>
        public async Task<List<string>> GetTypesAsync(string? value)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();

                IQueryable<ExpenseTracker> query = db.ExpenseTrackers.AsNoTracking();

                if (!string.IsNullOrEmpty(value))
                {
                    query = query.Where(e => e.Category.ToLower().Contains(value.ToLower()));
                }

                return await query
                    .Select(e => e.Type)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Log($"Error in GetTypesAsync", ex, $"Value: {value}");
                throw;
            }
        }

        public async Task<List<string>> GetTypesAsync(IReadOnlyCollection<string>? categories)
        {
            try
            {
                if (categories == null || !categories.Any())
                {
                    return await GetTypesAsync(string.Empty);
                }

                using var db = await _db.CreateDbContextAsync();

                return await db.ExpenseTrackers
                    .AsNoTracking()
                    .Where(e => categories.Contains(e.Category)) // Fast SQL "IN" clause
                    .Select(e => e.Type)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Log($"Error in GetTypesAsync (collection)", ex);
                throw;
            }
        }

        public async Task<List<string>> GetPlacesAsync(IReadOnlyCollection<string>? categories,
            IReadOnlyCollection<string>? types)
        {
            try
            {
                bool hasCategories = categories != null && categories.Any();
                bool hasTypes = types != null && types.Any();

                if (!hasCategories && !hasTypes)
                    return await GetPlacesAsync(string.Empty);

                using var db = await _db.CreateDbContextAsync();
                var query = db.ExpenseTrackers.AsNoTracking();

                if (hasCategories)
                    query = query.Where(e => categories!.Contains(e.Category));

                if (hasTypes)
                    query = query.Where(e => types!.Contains(e.Type));

                return await query
                    .Where(e => !string.IsNullOrEmpty(e.Place))
                    .Select(e => e.Place.Trim())
                    .Distinct()
                    .OrderBy(e => e)
                    .ToListAsync();
            }
            catch (Exception)
            {
                _loggerService.Log("Error in GetPlacesAsync (Dual Collections)");
                throw;
            }
        }

        /// <summary>
        /// Gets distinct places (for an expense type if passed) for UI autocomplete.
        /// </summary>
        public async Task<List<string>> GetPlacesAsync(string? value)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();

                IQueryable<ExpenseTracker> query = db.ExpenseTrackers.AsNoTracking();

                if (!string.IsNullOrEmpty(value))
                {
                    query = query.Where(e => e.Type.ToLower().Contains(value.ToLower()));
                }

                return await query
                    .Select(e => e.Place)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Log($"Error in GetPlacesAsync", ex, $"Value: {value}");
                throw;
            }
        }

        /// <summary>
        /// Gets distinct transaction details/notes for UI autocomplete.
        /// </summary>
        public async Task<List<string>> GetDetailsAsync(string? value)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();

                IQueryable<ExpenseTracker> query = db.ExpenseTrackers.AsNoTracking();

                if (!string.IsNullOrEmpty(value))
                {
                    query = query.Where(e => EF.Functions.Like(e.Detail, $"%{value}%"));
                }

                return await query
                    .Select(e => e.Detail)
                    .Distinct()
                    .OrderBy(e => e)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Log($"Error in GetDetailsAsync", ex, $"Value: {value}");
                throw;
            }
        }

        /// <summary>
        /// Gets unique trip destinations associated with expenses.
        /// </summary>
        public async Task<List<string?>> GetTripsAsync(string? value)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();

                IQueryable<ExpenseTracker> query = db.ExpenseTrackers
                    .AsNoTracking()
                    .Where(x => x.TripDestination != null);

                if (!string.IsNullOrEmpty(value))
                {
                    query = query.Where(e => EF.Functions.Like(e.TripDestination, $"%{value}%"));
                }

                return await query
                    .Select(e => e.TripDestination)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Log($"Error in GetTripsAsync", ex, $"Value: {value}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves records marked for Auto-Payment.
        /// </summary>
        /// <param name="isUpdate">If true, filters only for the current calendar month.</param>
        public async Task<List<ExpenseTracker>> GetAutoPayments(bool isUpdate = false)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();
                var autoPayments = await db.ExpenseTrackers
                    .AsNoTracking()
                    .Where(e => e.AutoPay != null && e.AutoPayDate != null)
                    .OrderBy(e => e.AutoPayDate)
                    .ToListAsync();

                if (isUpdate)
                {
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var start = new DateOnly(today.Year, today.Month, 1);
                    var end = start.AddMonths(1).AddDays(-1);
                    var currentMonthPayments = autoPayments.Where(e =>
                        e.AutoPay.HasValue && e.AutoPay == 1 && (e.AutoPayDate >= start && e.AutoPayDate <= end) ||
                        e.AutoPayDate <= today).ToList();
                    return currentMonthPayments;
                }

                return autoPayments;
            }
            catch (Exception ex)
            {
                _loggerService.Log($"Error in GetAutoPayments", ex);
                throw;
            }
        }

        /// <summary>
        /// Updates the status or date of an auto-payment entry.
        /// </summary>
        public async Task<int> UpdateAutoPaymentsAsync(ExpenseTracker autoPayment)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();
                db.ExpenseTrackers.Update(autoPayment);
                return await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _loggerService.Log($"Error in UpdateAutoPaymentsAsync", ex, JsonSerializer.Serialize(autoPayment));
                throw;
            }
        }

        /// <summary>
        /// Returns the next AutoPay date from a DateTime picker value.
        /// </summary>
        public DateTime GetNextDate(DateTime date, string frequency)
            => GetNextDate(DateOnly.FromDateTime(date), frequency).ToDateTime(TimeOnly.MinValue);

        /// <summary>
        /// Returns the next AutoPay date for the given frequency code (D/W/B/M/Q/H/Y).
        /// </summary>
        public DateOnly GetNextDate(DateOnly date, string frequency)
        {
            return frequency switch
            {
                Constants.AutoPay.Daily => date.AddDays(1),
                Constants.AutoPay.Weekly => date.AddDays(7),
                Constants.AutoPay.BiWeekly => date.AddDays(14),
                Constants.AutoPay.Monthly => date.AddMonths(1),
                Constants.AutoPay.Quarterly => date.AddMonths(3),
                Constants.AutoPay.HalfYearly => date.AddMonths(6),
                Constants.AutoPay.Yearly => date.AddYears(1),
                _ => throw new ArgumentException($"Invalid frequency: {frequency}")
            };
        }

        /// <summary>
        /// Calculates total spending and total income for the current year or month.
        /// Note: "Income" is identified strictly by the category name "Income".
        /// </summary>
        /// <param name="isCurrentMonth">If true, restricts sums to current month; otherwise, sums the whole year.</param>
        public async Task<(decimal expense, decimal income)> GetTotalExpenseAndIncomeAsync(bool? isCurrentMonth = false)
        {
            try
            {
                using var db = await _db.CreateDbContextAsync();

                if (isCurrentMonth == true)
                {
                    var currentMonthExpense = await db.ExpenseTrackers
                        .Where(e => e.Category != "Income" && e.Date.Year == DateTime.Now.Year &&
                                    e.Date.Month == DateTime.Now.Month)
                        .SumAsync(e => (decimal?)e.Price) ?? 0;
                    var currentMonthIncome = await db.ExpenseTrackers
                        .Where(e => e.Category == "Income" && e.Date.Year == DateTime.Now.Year &&
                                    e.Date.Month == DateTime.Now.Month)
                        .SumAsync(e => (decimal?)e.Price) ?? 0;
                    return (Math.Round(currentMonthExpense, 2), Math.Round(currentMonthIncome, 2));
                }
                else
                {
                    var totalExpense = await db.ExpenseTrackers
                        .Where(e => e.Category != "Income" && e.Date.Year == DateTime.Now.Year)
                        .SumAsync(e => (decimal?)e.Price) ?? 0;

                    var totalIncome = await db.ExpenseTrackers
                        .Where(e => e.Category == "Income" && e.Date.Year == DateTime.Now.Year)
                        .SumAsync(e => (decimal?)e.Price) ?? 0;

                    return (Math.Round(totalExpense, 2), Math.Round(totalIncome, 2));
                }
            }
            catch (Exception ex)
            {
                _loggerService.Log($"Error in GetTotalExpenseAndIncomeAsync", ex);
                throw;
            }
        }

        public async Task<List<string>> GetPlacesForReceiptScanAsync()
        {
            using var db = await _db.CreateDbContextAsync();

            var places = await db.ExpenseTrackers.AsNoTracking()
                .Where(x => x.Place != null)
                .Order()
                .Select(s => s.Place)
                .Distinct()
                .ToListAsync();

            return places;
        }
    }
}