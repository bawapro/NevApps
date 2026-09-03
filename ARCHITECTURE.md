# NevApps Architecture

> **Status:** Public &nbsp;|&nbsp; **Database:** SQLite 3 &nbsp;|&nbsp; **Framework:** .NET 10 / MAUI Blazor Hybrid

![GitHub last commit](https://img.shields.io/github/last-commit/bawapro/NevApps?style=flat-square&color=blue)
![SQLite](https://img.shields.io/badge/Database-SQLite-003B57?style=flat-square&logo=sqlite&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)

Two projects in this repo:

| Project | Role |
| :--- | :--- |
| `NevDBClass` | EF Core models, `SqLiteDbContext`, migrations |
| `NevApps` | MAUI UI (MudBlazor), services, platform code |

The SQLite file lives on-device (`NevDB.db3`). Sunrise/sunset uses [sunrise-sunset.org](https://sunrise-sunset.org/api). Moon phase is computed on-device with CoordinateSharp.

---

## Database schema

```mermaid
erDiagram
    EXPENSE_TRACKERS {
        int Id PK
        date Date
        string Category
        string Type
        string Place
        decimal Price
        string Detail
        int AutoPay
        date AutoPayDate
        string Frequency
        int TripFlag
        string TripDestination
        int TripId
    }

    MILEAGE_TRACKERS {
        int Id PK
        date Date
        string Vehicle
        string Start
        string End
        int OdoStart
        int OdoEnd
        int Distance
        int Mileage
        string Detail
        string GasStation
        string FuelType
        decimal FuelPrice
        decimal FuelFilled
        decimal FuelRate
        decimal FuelMileage
        int TripFlag
    }

    REMINDERS {
        int Id PK
        string Name
        int Type
        date Date
        string Notes
        string Mah
        string Roj
    }

    SETTINGS {
        int Id PK
        string SettingKey
        string SettingValue
        int IsEnabled
    }

    EXPENSE_TRACKERS ||--o| UNIQUE_INDEX_EXPENSE : "Date+Category+Type+Place+Detail+Price"
    MILEAGE_TRACKERS ||--o| UNIQUE_INDEX_MILEAGE : "Date+Vehicle+OdoStart+OdoEnd+Start+End"
    REMINDERS ||--o| UNIQUE_INDEX_REMINDER : "Name+Type+Date"
```

`Reminder.Type` is stored as an int (`Anniversary`, `Birthday`, `Consecration`, `Death` — order is fixed). Dates are `DateOnly` stored as `yyyy-MM-dd` text.

---

## Services

```mermaid
classDiagram
direction TB
    class SqLiteDbContext {
        +DbSet~ExpenseTracker~ ExpenseTrackers
        +DbSet~MileageTracker~ MileageTrackers
        +DbSet~Reminder~ Reminders
        +DbSet~Setting~ Settings
    }

    class ExpenseTrackerService {
        +SearchExpenseAsync(string)
        +AddExpenseAsync(ExpenseTracker)
        +UpdateExpenseAsync(ExpenseTracker)
        +DeleteExpenseAsync(int)
        +GetAutoPayments()
    }

    class MileageTrackerService {
        +SearchMileageAsync(string)
        +AddMileageAsync(MileageTracker)
        +UpdateMileageAsync(MileageTracker)
        +CalculateFuelMileage(decimal, decimal)
    }

    class ReminderService {
        +GetAllRemindersAsync()
        +AddReminderAsync(Reminder)
        +UpdateReminderAsync(Reminder)
        +DeleteReminderAsync(int)
    }

    class SettingsService {
        +GetSettingsAsync()
        +SaveSettingsAsync(Setting[])
        +GetCurrencyIcon()
        +GetFeaturesAsync()
    }

    class BackupRestoreService {
        +CreateBackupAsync()
        +RestoreBackupAsync(string)
        +ClearEverything()
    }

    class ReceiptService {
        +ParseReceiptData(string, List~string~)
        +ParseGasReceiptData(string, List~string~)
        +PreprocessImageForOcrAsync(byte[])
    }

    class StateService {
        +SetTripState(bool, string)
        +SetVehicleIsEV(string, bool)
        +SetSunData(string, string, string)
    }

    ExpenseTrackerService ..> SqLiteDbContext
    MileageTrackerService ..> SqLiteDbContext
    ReminderService ..> SqLiteDbContext
    SettingsService ..> SqLiteDbContext
    BackupRestoreService ..> SqLiteDbContext
```

Receipt scan is on-device: `Plugin.Maui.OCR` + `ReceiptService` parsers.

`StateService` uses `Microsoft.Maui.Storage.Preferences` (trip flag, EV vehicles, cached sunrise/sunset). It is not SQLite.

---

## Navigation

Top app bar icons are shown only when the matching feature is enabled in Settings.

```mermaid
graph LR
    subgraph Bar [App bar]
        H[Home]
        E[Expenses]
        M[Mileage]
        C[Shahenshahi]
        P[Steps]
        S[Settings]
    end

    H --> H1[Dashboard widgets]
    H1 --> H2[Expense / mileage summaries]
    H1 --> H3[Auto-pay]
    H1 --> H4[Reminders]

    E --> E1[Add / Edit / View]

    M --> M1[Add / Edit / View]

    C --> C1[Calendar view]
    C --> C2[Reminders]
    C --> C3[Sunrise / sunset / moon phase]

    P --> P1[Step session]

    S --> S1[Apps / country / vehicles / categories]
    S --> S2[Backup restore clear]
```

There is no separate theme screen. Backup/restore is local only.

---

## External APIs

```mermaid
sequenceDiagram
    participant UI as NevApps
    participant SS as sunrise-sunset.org

    UI->>SS: GET /json?lat&lng&tzid
    SS-->>UI: local-time JSON
```

Shahenshahi dates, Muhurat, and moon phase are computed on-device (`ShenshaiCalendar`, `Muhurat`, CoordinateSharp).

---

## Build configurations

`Debug` and `Release`. Store signing is **not** in the csproj. Copy `Directory.Build.props.example` to gitignored `Directory.Build.props` on the machine that publishes.
