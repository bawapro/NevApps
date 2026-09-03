# NevApps

**.NET MAUI** + **Blazor Hybrid** app (MudBlazor) that groups several tools in one install: expenses, mileage, a Shahenshahi (Parsi Zoroastrian) calendar, and step tracking.

Built for daily personal use, then opened as a public snapshot of the 2.x MAUI rewrite (the earlier app used Fluent UI).

## Features

- **Expense tracker** — income/expenses, search, auto-pay, trip-linked entries, receipt scan
- **Mileage tracker** — trips, ICE vs EV fuel logic, gas-receipt scan
- **Shahenshahi calendar** — Roj/Mah view, Muhurat, sunrise/sunset and moon phase for the current location, reminders (anniversary, birthday, consecration, death)
- **Step tracker** — on-device session tracking (Android / iOS)
- **Dashboard** — summaries, auto-pay, reminders (Home)
- **Settings** — which apps are on, country/units/currency, vehicles, categories, local backup/restore

## Stack

- .NET 10 MAUI, MudBlazor, SQLite (EF Core)
- Data layer: in-repo `NevDBClass/`
- License: MIT

Architecture and schema: [ARCHITECTURE.md](ARCHITECTURE.md).

## Build

Requires the .NET 10 SDK and the MAUI workload.

Open `NevApps.slnx` in Rider, or:

```bash
dotnet restore NevApps.csproj
dotnet build NevApps.csproj -f net10.0-android -c Debug
```

Signed **Release** builds need a local `Directory.Build.props` (gitignored). Copy `Directory.Build.props.example` and fill in keystore / codesign values.

Receipt scan uses on-device OCR.

## Regional behavior

Country in Settings drives:

- **Distance** — km or miles
- **Fuel** — L/100km or MPG
- **Currency** — symbol/icon from the selected region
