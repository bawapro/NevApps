# NevApps 🚗💨

**NevApps** is a modular, high-performance mobile application built with **.NET MAUI** and **Blazor Hybrid**, featuring
a sleek, modern UI powered by **MudBlazor**.

Conceived as an "Apps within an App" ecosystem, NevApps centralizes diverse utilities—ranging from comprehensive vehicle
management to a specialized Zoroastrian calendar—into a single, cohesive experience.

This version is the result of over two years of personal daily use and iterative development. While the original
iteration utilized FluentUI, this 2.0 release represents a total overhaul of the UI/UX. My goal was to take a proven,
battle-tested tool and make it available to the public with an intuitive, user-friendly interface that handles complex
data management with ease.

The current store release is **3.0 (30)**. The history is in [RELEASES.md](RELEASES.md).

## ✨ Key Features

* **Expense Tracker:** Seamlessly manage income and expenses with high-speed search functionality. Includes an
  **Auto-Payment engine** to handle recurring costs automatically and deep integration with trip-based mileage expenses.
  An expense can use the home currency or an extra currency saved in Settings.
* **Mileage Tracker:** Log local and cross-border trips with ease. Features an **Intelligent Fuel Engine** that
  automatically toggles metrics and logic based on your vehicle type (Internal Combustion vs. EV).
* **Shahenshahi Calendar:** A unified view of the Parsi Zoroastrian calendar, featuring precise Sunrise/Sunset timings,
  moon phases, and **Muharat** calculations based on your specific city. **Reminders** track birthdays, anniversaries,
  consecrations, and Baj by Roj and Mah. The dashboard shows the upcoming weekday, and the list runs forward from
  today. The saved date stays beside the type.
* **Steps Tracker:** Optional daily step logging, shown in the same swipe bar as the other sections.
* **Smart Dashboard:** A centralized "Command Center" providing an immediate overview of current expenses, pending
  auto-payments, mileage statistics, and upcoming reminders. Announcements from NevAPI show on Home.
* **Receipt scan:** A photographed receipt can fill in an expense or a mileage fill.
* **User Empowerment & Support:**
    * **Customization:** Feature toggles, theme (including dark mode), and a mileage unit choice.
    * **Navigation:** Swipe between sections and pull to refresh.
    * **Data Integrity:** Built-in database backup and restore functionality.
    * **Developer Sync:** Feedback and issue logs stay on the phone that sent them. An admin reply opens under
      Support on that phone, and an issue log includes the exception text.

## 🛠️ Technical Stack

* **Framework:** .NET 10 MAUI (Android and iOS)
* **UI Library:** [MudBlazor](https://mudblazor.com/) (Component Library for Blazor)
* **Database:** SQLite with Entity Framework Core
* **API:** Custom NevAPI on Azure for announcements, feedback, and issue logs
* **Architecture:** Service-based architecture with dependency injection

## 📱 Regional Support

The app currently supports specialized logic for:

* **Currency** follows the country saved in Settings, including the currency icon and code. Extra currencies saved in
  Settings can be used on an expense.
* **Metric/Imperial** Kilometers and litres (L/100km), or miles and MPG. Choosing the United States turns imperial
  on; another country turns it off. The mileage unit toggle can override that until the country changes again.

## 🏗️ Project Architecture

NevApps 2.0 follows a decoupled architecture to ensure the data logic remains independent of the mobile UI components.

### 1. Data Layer (`NevDBClass`)

A dedicated Class Library project that handles all data persistence and business entities.

* **Models:** Core Entity Framework entities (ExpenseTracker, MileageTracker, Reminder, Settings).
* **Data:** The `SqLiteDbContext` and migration history. This allows the database logic to be reused or unit-tested
  outside of the MAUI environment.

### 2. UI & Logic Layer (`NevApps`)

The primary .NET MAUI Blazor Hybrid project.

* **Services:** The "Engine Room." Contains logic for DB & API communications, regional unit conversions
  (Metric/Imperial), and state management.
* **Pages:** MudBlazor-powered views. Each "App within the App" (Expenses, Mileage, Calendar, Steps) is siloed here
  for easy maintenance.
* **Shared:** `MainLayout` and `SectionShell` hold the app bar, swipe sections, and common UI.
* **Platforms:** Target-specific configuration for Android and iOS.

## ⚙️ Regional Logic & Units

The app features a custom **Regional Intelligence** system. Based on the user's selected country in `SettingsService`:

* **Distance:** Kilometers or miles. The country sets the default, and the mileage unit toggle can override it until
  the country changes again.
* **Fuel Economy:** Calculates **L/100km** (Metric) or **MPG** (Imperial).
* **Currency:** Switches the currency icon (for example Euro, dollar, or pound) from the country saved in Settings.
  Extra currencies saved in Settings can be used on an expense.

## Documentation

* [Architecture](ARCHITECTURE.md)
* [Framework reference](REFERENCE.md) for the .NET classes, properties, and methods used in the solution
* [Data specs](DATA-SPECS.md) for the Settings table and State preferences
* [iPhone debug and release](IOS.md)
* [Releases](RELEASES.md)

---
*NevApps is built with ❤️ and .NET MAUI.*
