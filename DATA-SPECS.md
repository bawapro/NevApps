# Data Specs

Stored values that control NevApps.

## Settings

SQLite table `Settings` in `NevDB.db3` (`NevDBClass.Models.Setting`).
`Name` is `SettingKey`. `Value` is `SettingValue`, plus `IsEnabled` where that
column is the switch or the read state. One name can have many rows.
Home is shown when Expense, Mileage, or Shenshai is on. Settings is always shown.

Rows through `nevapi` are written when Settings is saved. `extra_currency` is
replaced in that same save. The rows after that are written by later actions.

| Name | Value | Purpose | Controls |
| --- | --- | --- | --- |
| `expensetracker` | Empty. `IsEnabled` 1 on, 0 off | Expense feature switch | Expense section. If all four features are saved off, this one is stored on |
| `mileagetracker` | Empty. `IsEnabled` 1 on, 0 off | Mileage feature switch | Mileage section |
| `shenshaicalendar` | Empty. `IsEnabled` 1 on, 0 off | Shenshai feature switch | Shenshai section |
| `stepstracker` | Empty. `IsEnabled` 1 on, 0 off | Steps feature switch | Steps section |
| `country` | English country name, or `NA` | Region chosen in Settings | `United States` uses miles and US gallons. Any other name uses kilometres and litres. `NA` blocks Expense until a real country is saved. Also picks the currency culture, such as `en-CA` |
| `currency` | ISO code, such as `CAD`, or empty with `NA` | Home currency for that country | Nav currency icon and home-currency amounts. An empty code is read as `CAD` |
| `extra_currency` | One row per ISO code, such as `USD`. `IsEnabled` 1 | Extra currencies chosen in Settings | Currencies an expense can use besides the home currency. Kept only for `AUD`, `CAD`, `CHF`, `CNY`, `EUR`, `GBP`, `HKD`, `INR`, `JPY`, `NZD`, `RUB`, `SGD`, `USD`, and only when different from home |
| `category` | Comma-separated names, such as `Car,Food,Shopping`, or empty. `IsEnabled` 1 | Expense categories chosen in Settings | A filled value is the fixed list (`Bank`, `Car`, `Food`, `Medical`, `Payment`, `Shopping`, `Travel`). Empty is Custom mode, which offers categories already used on expenses |
| `vehicle` | One row per name, such as `Civic`. `IsEnabled` 1 | Vehicles entered in Settings | Mileage vehicle list. The placeholder `NA` is not stored. No rows blocks Mileage entry |
| `startlocation` | `Home` or empty. `IsEnabled` 1 | Default start of a trip | `Home` prefills the start and labels the other end Destination. Empty leaves both ends for the user |
| `nevapi` | Empty. `IsEnabled` 1 | Written on every settings save | Nothing. The API host is `Constants.APIUrl` |
| `{vehicle name}` | Path of the PNG under app data, or empty. `IsEnabled` 1 | Picture for that vehicle. The name matches a `vehicle` value, such as `Civic` | Image on the mileage summary. Empty removes it. The file itself is not in the table |
| `userid` | 32-character id, created once. `IsEnabled` 1 | Id for this install | Sent with feedback and issues |
| `feedback-{userid}` | The posted feedback text. `IsEnabled` 1 | Record that feedback was sent | Presence of the row blocks a second feedback post |
| `dev_feedback` | Developer reply text. `IsEnabled` 1 unread, 0 read | Reply to the user's feedback | Unread badge. Marking it read keeps the text and sets `IsEnabled` to 0 |
| `issue-{userid}` | The posted log text. `IsEnabled` 1 while active | A reported issue | Issue notification. Marking issues read sets `IsEnabled` to 0 on every name containing `issue` |
| `dev_issuereply` | First developer reply text. `IsEnabled` 1 while waiting | Reply to a reported issue | Waiting-reply badge. A later reply does not replace the stored text. Marking issues read sets `IsEnabled` to 0 |
| `announcement` | One row per announcement id. `IsEnabled` 1 unseen, 0 opened | Which announcements the user has opened | Home badge while any row is still unseen |

## State

Device preferences in `StateService` (MAUI Preferences). `Name` is the preference key. These survive an app restart. The last two rows are memory only and reset when the process ends.

| Name | Value | Purpose | Controls |
| --- | --- | --- | --- |
| `AppTheme` | `System`, `Light`, or `Dark`. Missing key reads as `System` | Theme chosen in Settings | Light, dark, or the OS theme for the whole app |
| `TripFlag` | `true` or `false`. Missing key reads as `false` | Whether a trip is open | Expense add continues that trip. Cleared together with `TripName` |
| `TripName` | Destination text, or empty | Name of the open trip | Prefilled trip destination on the next expense. Stored on the expense as its trip destination while the flag is on |
| `EV_{vehicle name}` | `true` or `false`. One key per vehicle, name trimmed and lowercased, such as `EV_civic`. Missing key reads as `false` | That vehicle is electric | Mileage entry uses charge fields (kWh) instead of fuel fields. Mileage summary marks the vehicle as EV. Removed when that vehicle is removed |
| `IsNevAPIOn` | `true` or `false`. Missing key reads as `true` | Whether the NevAPI host answered | Support panel in Settings for a non-admin user. Set `false` when a NevAPI call fails. Set `true` when the health check returns |
| `Sun_Sunrise` | Local sunrise time, such as `6:42 AM`, or empty | Cached sunrise for today | Shown on the Shenshai sunrise/sunset screen. Kept when a later fetch fails |
| `Sun_Sunset` | Local sunset time, or empty | Cached sunset for today | Shown with sunrise. Kept when a later fetch fails |
| `Sun_City` | City name, or empty. Missing key reads as `Unknown` | City for the cached sun times | Shown with the sun times |
| `Sun_LastUpdate` | Date `yyyy-MM-dd` | Day those sun times were fetched | A date other than today causes a new location and sunrise/sunset fetch |
| `NavrozYear` | Calendar year, such as `2026`. Missing key reads as `0` | Year the Navroz greeting was last shown | Greeting on the Shenshai calendar once per year, during the first month. Stores the current year so it does not show again until the next year |
| `DashboardReload` | `true` or `false`. Memory only, starts `false` | Expense or mileage data changed | Home reloads the dashboard once, then sets this back to `false` |
| `HasAdminSupportEntries` | `true` or `false`. Memory only, starts `false` | Admin build found cloud feedback or issue entries | Support section in the nav |
