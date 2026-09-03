using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NevApps.Classes;
using NevApps.Interfaces;
using NevApps.Services;
using MudBlazor;
using MudBlazor.Services;
using NevDBClass.Data;
using Plugin.Maui.OCR;

namespace NevApps
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });
            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
                config.SnackbarConfiguration.VisibleStateDuration = 3000;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.NewestOnTop = true;
            });

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, Constants.DbName);
            builder.Services.AddDbContextFactory<SqLiteDbContext>(options => options.UseSqlite($"Filename={dbPath}",
                sqliteOptions => sqliteOptions.MigrationsAssembly("NevDBClass"))
            );

            builder.Services.AddSingleton<LoggerService>();
            builder.Services.AddScoped<ExpenseTrackerService>();
            builder.Services.AddScoped<MileageTrackerService>();
            builder.Services.AddScoped<SettingsService>();
            builder.Services.AddSingleton<StateService>();
            builder.Services.AddSingleton<ReminderService>();
            builder.Services.AddSingleton<BackupRestoreService>();
            builder.Services.AddSingleton(OcrPlugin.Default);
            builder.Services.AddSingleton<ReceiptService>();
           
#if ANDROID
            builder.Services.AddSingleton<IStepTrackerService, NevApps.StepTrackerService>();
#elif IOS
            builder.Services.AddSingleton<IStepTrackerService, NevApps.Platforms.iOS.StepTrackerService>();
#endif
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            var logger = app.Services.GetRequiredService<LoggerService>();
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                if (ex != null)
                {
                    logger.Log("UNHANDLED AppDomain", ex);
                }
            };
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                logger.Log("UNHANDLED Task Exception", args.Exception);
                args.SetObserved(); // prevents crash on some platforms
            };

#if ANDROID
            Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
            {
                logger.Log("UNHANDLED Android", args.Exception);
                args.Handled = true; // prevents immediate crash (Android allows this)
            };
#endif

#if IOS
            ObjCRuntime.Runtime.MarshalManagedException += (_, args) =>
            {
                args.ExceptionMode = ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode;
            };
#endif


            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SqLiteDbContext>();
            try
            {
                db.Database.Migrate(); // Applies all pending migrations automatically
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1) // general SQLite error
            {
                logger.Log("DB Migration Error", ex);
            }
            catch (Exception ex)
            {
                logger.Log("Unexpected migration error", ex);
            }

            return app;
        }
    }
}