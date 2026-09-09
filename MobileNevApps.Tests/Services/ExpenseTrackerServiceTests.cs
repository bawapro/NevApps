using NevApps.Classes;
using MobileNevApps.Tests.Helpers;
using NevDBClass.Models;

namespace MobileNevApps.Tests.Services;

public class ExpenseTrackerServiceTests : IDisposable
{
    private readonly AppTestHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task AddAndGetAllExpenses_ReturnsInsertedRowsNewestFirst()
    {
        var older = AppTestHarness.Expense(place: "A", date: new DateOnly(2026, 1, 1), price: 10);
        var newer = AppTestHarness.Expense(place: "B", date: new DateOnly(2026, 2, 1), price: 20);

        Assert.Equal(1, await _harness.Expenses.AddExpenseAsync(older));
        Assert.Equal(1, await _harness.Expenses.AddExpenseAsync(newer));

        var all = await _harness.Expenses.GetAllExpensesAsync();
        Assert.Equal(2, all.Count);
        Assert.Equal("B", all[0].Place);
        Assert.Equal("A", all[1].Place);
    }

    [Fact]
    public async Task SearchExpenseAsync_EmptyString_ExcludesAutoPaymentsAndLimitsToTen()
    {
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(place: "Normal", autoPay: 0));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(
            place: "Auto", category: "Bank", type: "Bill", detail: "Rent",
            autoPay: 1, autoPayDate: DateOnly.FromDateTime(DateTime.Today), frequency: Constants.AutoPay.Monthly,
            price: 100));

        var results = await _harness.Expenses.SearchExpenseAsync(" ");
        Assert.Single(results);
        Assert.Equal("Normal", results[0].Place);
    }

    [Fact]
    public async Task SearchExpenseAsync_MatchesCategoryTypePlaceAndDetail()
    {
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(category: "Food", type: "Cafe", place: "Starbucks", detail: "Latte"));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(category: "Car", type: "Fuel", place: "Shell", detail: "Fill"));

        var byPlace = await _harness.Expenses.SearchExpenseAsync("star");
        var byDetail = await _harness.Expenses.SearchExpenseAsync("Fill");

        Assert.Single(byPlace);
        Assert.Equal("Starbucks", byPlace[0].Place);
        Assert.Single(byDetail);
        Assert.Equal("Shell", byDetail[0].Place);
    }

    [Fact]
    public async Task UpdateAndDeleteExpense_ModifyAndRemoveRows()
    {
        var expense = AppTestHarness.Expense(price: 5);
        await _harness.Expenses.AddExpenseAsync(expense);

        expense.Price = 9.99m;
        Assert.Equal(1, await _harness.Expenses.UpdateExpenseAsync(expense));

        var updated = (await _harness.Expenses.GetAllExpensesAsync()).Single();
        Assert.Equal(9.99m, updated.Price);

        Assert.Equal(1, await _harness.Expenses.DeleteExpenseAsync(updated.Id));
        Assert.Empty(await _harness.Expenses.GetAllExpensesAsync());
        Assert.Equal(0, await _harness.Expenses.DeleteExpenseAsync(updated.Id));
    }

    [Fact]
    public async Task GetExpenseObjByTripIdAsync_ReturnsLinkedExpense()
    {
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(tripId: 42, tripDestination: "Montreal"));
        var found = await _harness.Expenses.GetExpenseObjByTripIdAsync(42);
        var missing = await _harness.Expenses.GetExpenseObjByTripIdAsync(99);

        Assert.NotNull(found);
        Assert.Equal("Montreal", found!.TripDestination);
        Assert.Null(missing);
    }

    [Fact]
    public async Task GetDataByPlaceAsync_ReturnsLatestMatchingPlace()
    {
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(
            place: "Costco", category: "Food", type: "Groceries", detail: "Old", date: new DateOnly(2026, 1, 1)));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(
            place: "Costco", category: "Shopping", type: "Store", detail: "New", date: new DateOnly(2026, 3, 1)));

        var (category, type, detail) = await _harness.Expenses.GetDataByPlaceAsync("Costco");
        Assert.Equal("Shopping", category);
        Assert.Equal("Store", type);
        Assert.Equal("New", detail);
    }

    [Fact]
    public async Task GetDataByPlaceAsync_UnknownPlace_ReturnsFallback()
    {
        var (category, type, detail) = await _harness.Expenses.GetDataByPlaceAsync("Nowhere");
        Assert.Equal("Shopping", category);
        Assert.Equal("Store", type);
        Assert.Equal("Nowhere", detail);
    }

    [Fact]
    public async Task GetCategoriesAsync_FromSettingsAndFromExpenses()
    {
        await _harness.Settings.SaveSettingsAsync(AppTestHarness.Setting("category", "Food, Car, Medical"));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(category: "Travel"));

        var fromSettings = await _harness.Expenses.GetCategoriesAsync(null, fromDB: false);
        var fromDb = await _harness.Expenses.GetCategoriesAsync(null, fromDB: true);
        var filtered = await _harness.Expenses.GetCategoriesAsync("trav", fromDB: true);

        Assert.Equal(new[] { "Car", "Food", "Medical" }, fromSettings);
        Assert.Contains("Travel", fromDb);
        Assert.Equal(new[] { "Travel" }, filtered);
    }

    [Fact]
    public async Task GetTypesAndPlaces_FilterByCategoryAndType()
    {
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(category: "Food", type: "Cafe", place: "Aroma"));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(category: "Car", type: "Fuel", place: "Shell"));

        var foodTypes = await _harness.Expenses.GetTypesAsync(new[] { "Food" });
        var carPlaces = await _harness.Expenses.GetPlacesAsync(new[] { "Car" }, new[] { "Fuel" });
        var details = await _harness.Expenses.GetDetailsAsync("shop");

        Assert.Equal(new[] { "Cafe" }, foodTypes);
        Assert.Equal(new[] { "Shell" }, carPlaces);
        Assert.Contains("Weekly shop", details);
    }

    [Fact]
    public async Task GetTripsAsync_ReturnsDistinctDestinations()
    {
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(place: "Hotel", tripDestination: "Paris", price: 1));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(place: "Train", tripDestination: "Paris", price: 2, detail: "Rail"));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(place: "Home", tripDestination: null, price: 3, detail: "None"));

        var trips = await _harness.Expenses.GetTripsAsync(null);
        var filtered = await _harness.Expenses.GetTripsAsync("par");

        Assert.Equal(new[] { "Paris" }, trips);
        Assert.Equal(new[] { "Paris" }, filtered);
    }

    [Fact]
    public async Task GetAutoPayments_FiltersCurrentMonthWhenRequested()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(
            place: "ThisMonth", autoPay: 1, autoPayDate: today, frequency: Constants.AutoPay.Monthly, price: 10, detail: "Now"));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(
            place: "NextYear", autoPay: 1, autoPayDate: today.AddYears(1), frequency: Constants.AutoPay.Yearly, price: 20, detail: "Later"));

        var all = await _harness.Expenses.GetAutoPayments();
        var current = await _harness.Expenses.GetAutoPayments(isUpdate: true);

        Assert.Equal(2, all.Count);
        Assert.Contains(current, e => e.Place == "ThisMonth");
        Assert.DoesNotContain(current, e => e.Place == "NextYear");
    }

    [Theory]
    [InlineData(Constants.AutoPay.Daily, 1)]
    [InlineData(Constants.AutoPay.Weekly, 7)]
    [InlineData(Constants.AutoPay.BiWeekly, 14)]
    public void GetNextDate_AddsExpectedDays(string frequency, int days)
    {
        var start = new DateOnly(2026, 1, 15);
        Assert.Equal(start.AddDays(days), _harness.Expenses.GetNextDate(start, frequency));
    }

    [Theory]
    [InlineData(Constants.AutoPay.Monthly, 1)]
    [InlineData(Constants.AutoPay.Quarterly, 3)]
    [InlineData(Constants.AutoPay.HalfYearly, 6)]
    [InlineData(Constants.AutoPay.Yearly, 12)]
    public void GetNextDate_AddsExpectedMonths(string frequency, int months)
    {
        var start = new DateOnly(2026, 1, 31);
        Assert.Equal(start.AddMonths(months), _harness.Expenses.GetNextDate(start, frequency));
    }

    [Fact]
    public void GetNextDate_InvalidFrequency_Throws()
    {
        Assert.Throws<ArgumentException>(() => _harness.Expenses.GetNextDate(DateOnly.FromDateTime(DateTime.Today), "X"));
    }

    [Fact]
    public async Task GetTotalExpenseAndIncomeAsync_SplitsIncomeFromSpending()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(category: "Food", price: 40, date: today));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(category: "Income", type: "Salary", place: "Work", price: 100, date: today, detail: "Pay"));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(
            category: "Food", price: 5, date: today.AddYears(-1), place: "Old", detail: "Last year"));

        var (yearExpense, yearIncome) = await _harness.Expenses.GetTotalExpenseAndIncomeAsync();
        var (monthExpense, monthIncome) = await _harness.Expenses.GetTotalExpenseAndIncomeAsync(isCurrentMonth: true);

        Assert.Equal(40m, yearExpense);
        Assert.Equal(100m, yearIncome);
        Assert.Equal(40m, monthExpense);
        Assert.Equal(100m, monthIncome);
    }

    [Fact]
    public async Task GetPlacesForReceiptScanAsync_ReturnsDistinctPlaces()
    {
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(place: "Costco"));
        await _harness.Expenses.AddExpenseAsync(AppTestHarness.Expense(place: "Costco", type: "Wholesale", price: 1, detail: "Bulk"));

        var places = await _harness.Expenses.GetPlacesForReceiptScanAsync();
        Assert.Equal(new[] { "Costco" }, places);
    }
}
