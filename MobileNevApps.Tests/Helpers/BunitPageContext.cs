using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NevApps.Interfaces;
using NevApps.Services;
using MudBlazor.Services;
using NevDBClass.Data;
using Plugin.Maui.OCR;

namespace MobileNevApps.Tests.Helpers;

/// <summary>
/// Shared bUnit host that wires the same services the MAUI pages inject via _Imports.razor.
/// Uses IAsyncLifetime so MudBlazor's IAsyncDisposable services shut down cleanly.
/// </summary>
public abstract class BunitPageContext : IAsyncLifetime
{
    private BunitContext _ctx = null!;

    internal AppTestHarness Harness { get; private set; } = null!;
    protected StateService State { get; private set; } = null!;

    protected BunitContext Ctx => _ctx;

    public Task InitializeAsync()
    {
        Harness = new AppTestHarness();
        State = new StateService(new InMemoryPreferences());

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        _ctx.Services.AddSingleton<IDbContextFactory<SqLiteDbContext>>(Harness.Db);
        _ctx.Services.AddSingleton(Harness.Logger);
        _ctx.Services.AddSingleton(Harness.Expenses);
        _ctx.Services.AddSingleton(Harness.Mileage);
        _ctx.Services.AddSingleton(Harness.Settings);
        _ctx.Services.AddSingleton(Harness.Reminders);
        _ctx.Services.AddSingleton(Harness.Backup);
        _ctx.Services.AddSingleton(State);
        _ctx.Services.AddSingleton(new ReceiptService());
        _ctx.Services.AddSingleton<IOcrService>(new FakeOcrService());
        _ctx.Services.AddSingleton<IStepTrackerService>(new FakeStepTrackerService());

        SeedConfiguredProfile();
        return Task.CompletedTask;
    }

    protected IRenderedComponent<T> Render<T>(Action<ComponentParameterCollectionBuilder<T>>? parameterBuilder = null)
        where T : IComponent
        => parameterBuilder is null ? _ctx.Render<T>() : _ctx.Render(parameterBuilder);

    protected void SeedConfiguredProfile()
    {
        using var db = Harness.Db.CreateDbContext();
        db.Settings.RemoveRange(db.Settings);
        db.SaveChanges();

        db.Settings.AddRange(
            AppTestHarness.Setting("country", "Canada"),
            AppTestHarness.Setting("currency", "CAD"),
            AppTestHarness.Setting("category", "Food, Car, Shopping"),
            AppTestHarness.Setting("vehicle", "Civic"),
            AppTestHarness.Setting("expensetracker", "1"),
            AppTestHarness.Setting("mileagetracker", "1"),
            AppTestHarness.Setting("shenshaicalendar", "1"),
            AppTestHarness.Setting("stepstracker", "1"));
        db.SaveChanges();
    }

    protected void SeedFirstLaunch()
    {
        using var db = Harness.Db.CreateDbContext();
        db.Settings.RemoveRange(db.Settings);
        db.ExpenseTrackers.RemoveRange(db.ExpenseTrackers);
        db.MileageTrackers.RemoveRange(db.MileageTrackers);
        db.Reminders.RemoveRange(db.Reminders);
        db.SaveChanges();
    }

    public async Task DisposeAsync()
    {
        Harness.Dispose();
        await _ctx.DisposeAsync();
    }
}
