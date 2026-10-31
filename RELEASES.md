# Release History

Release date is the day the build was pushed to the store. Commit date comes from git. Version and build are the ones recorded for that store release.

## 3.0 (30)

**Implemented Themes, Swiping, Multi-currency, Pull-up-refresh; Fixed Feedback; Enhanced ScanReceipt, Dashboard, Reminders & Settings.**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-09-28 | 2026-09-27 | 3.0 | 30 |

Current app version (`ApplicationDisplayVersion` 3.0.0). This is the public shape of the work on `bug/feedback`
after the Reminders announcement.

### Navigation and appearance

- Swipe between Home, Expenses, Mileage, Shahenshahi, Steps, and Settings.
- Theme choice, including dark mode.
- Pull to refresh on the main sections.
- A startup loading screen while Blazor initializes.
- iPhone scrolling fixes on add and view pages.

### Money and trips

- Currency icon and code follow the country saved in Settings.
- A scanned receipt can fill in an expense.
- Mileage dashboard cards are ordered from the latest fill.

### Reminders

- The dashboard shows the upcoming weekday, such as Mon, Sep 28.
- The View list runs forward from today using the next Roj-Mah date.
- The saved date sits beside the type, such as Birthday – Sep 28, 1990.

### Support

- Feedback and its reply stay on the install that sent them.
- After an admin reply, Support shows **View Feedback** and keeps the reply readable after it is opened.
- The unread mark on Settings clears once the reply is read.
- An issue log sent to admin includes the exception type, message, and stack trace.
- The admin Support icon and tab counts follow the live lists and hide when nothing is waiting.
- NevAPI treats an issue upload as a duplicate only when that exact file name already exists.

## 2.5 (25)

**Code cleanup & refactoring done before new features/changes are added.**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-08-31 | 2026-09-01 | 2.5 | 25 |

- Dead code removed.
- AutoPay date is updated from one service call instead of from the pages.

## 2.4 (24)

**New Reminders Feature, Bug Fix in AutoPay Update, Migration Insert on Restore & Broadcast Notification Fix on Read**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-08-05 | 2026-07-05 | 2.4 | 24 |

- Reminders added.
- AutoPay update bug fixed.
- Restore inserts the migration row.
- Reading a broadcast no longer leaves it marked unread.

## 1.9 (19)

**Steps Tracker**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-07-16 | 2026-07-21 | 1.9 | 19 |

- Step tracking on Android and iOS.
- Shenshai code refactoring in the same change.

## 1.6 (16)

**Moon Phases**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-06-29 | 2026-07-05 | 1.6 | 16 |

- Moon phases on the Shenshai calendar.
- Muhurat opens in a dialog.
- Calendar UI and logic updated.

## 1.4 (14)

**Scan Receipts**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-06-25 | 2026-06-26 | 1.4 | 14 |

- A photographed receipt can be scanned into Expense and Mileage.

## 1.2 (12)

**Custom Filter Implementation**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-06-09 | 2026-06-09 | 1.2 | 12 |

- Custom filters on the tracker lists.
- Turning AutoPay off clears its fields.

## 1.1 (11)

**First Commit from MacOS. Changes wrt showing only numbers and . in case of decimal in keypad instead of Qwerty type in Expense & Mileage. Removed Catalyst, Windows blocks from Program.cs. Added iOS block for getting the app release date in View Settings.**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-05-20 | 2026-05-20 | 1.1 | 11 |

- Expense and Mileage decimal fields use a numeric keypad.
- Mac Catalyst and Windows startup blocks removed.
- Settings shows the iOS release date.

## 1.0.9 (10)

**iOS Testing on Apple Platform & Launching on App Store**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
|  | 2026-05-19 | 1.0.9 | 10 |

- Tested on iOS and launched on the App Store.

## 1.0.8 (9)

**Multiple Announcements, Remove AutoPay Headers**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-05-12 | 2026-05-12 | 1.0.8 | 9 |

- More than one announcement can be shown.
- AutoPay headers removed.

## 1.0.7 (8)

**Muhurat period fix, Dialog window BackDropClick set to false, FuelRate N2 to N3 and Edge-to-Edge addition in Android OnCreate**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-04-16 | 2026-04-16 | 1.0.7 | 8 |

- Muhurat period calculation fixed.
- Dialogs stay open when the backdrop is clicked.
- Fuel rate shows three decimal places.
- Android draws edge to edge.

## Google PlayStore Launch Version

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-04-14 | 2026-04-14 |  |  |

This release marks the transition from a local, personal tool to a public-ready fully-featured Mobile App.

### 🛠️ Core Infrastructure & UI

- **UI Redesign:** Implemented complete MudBlazor overhaul including custom pagination and swiping logic.
- **Dynamic Layouts:** Integrated Shenshai UI, home page layout changes, and dynamic support pane visibility.
- **Performance:** Conducted stress tests with 100, 1,000, and 10,000 records to ensure database stability.
- **Admin Tools:** Added admin-mode toggles for MudBadges and issue management.

### 🚗 Vehicle & Mileage Intelligence

- **EV Support:** Implemented specialized logic for Electric Vehicles in both Settings and Mileage Tracking.
- **Unit Precision:** Fixed mileage conversion bugs, volume units for EVs, and missing distance unit displays.
- **Fuel Economy:** Fully integrated Fuel-Mileage calculations and dashboard visualization.

### 💰 Expense & Auto-Pay Engine

- **Automation:** Built logic to disable/enable fields based on main selections and integrated Auto-Pay with Trip logic.
- **Financial Features:** Included Monthly Income tracking and refined 'Last Payment' date logic.

### 📅 Zoroastrian (Shahenshahi) Calendar

- **Accuracy:** Fixed Muhurat calculations and implemented 'Roj' cut-off logic.
- **Regional:** Integrated Sunrise-Sunset timings based on user location.

### ☁️ Cloud & Support

- **Connectivity:** Implemented API management for "down/offline" scenarios.
- **Feedback Loop:** Added error messaging for users and one-to-one developer communication.

## 1.0.6 (7)

**Version Change (Production Release)**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
|  | 2026-04-12 | 1.0.6 | 7 |

- Production release number set to 1.0.6, build 7.

## 1.0.5 (6)

**Income color changes in AutoPay and Expense, Removed income from CategorySelection, Code Cleanup**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-04-12 | 2026-04-12 | 1.0.5 | 6 |

- Income amounts use a distinct color in AutoPay and Expense.
- Income removed from category selection.
- Code cleanup.

## 1.0.4 (5)

**Changes wrt Akshay's comments: Remove income from category selection, show list of vehicles under autocomplete, empty dashboard message, circular progress bar in announcements.**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-04-05 | 2026-04-05 | 1.0.4 | 5 |

- Income removed from the category list.
- Saved vehicles are listed under the autocomplete.
- An empty dashboard shows a message.
- Announcements show a circular progress indicator.

## 1.0.3 (4)

**Code cleanup, Fixes for Summary Mileage Distance values, EV labels, and minor corrections. Fetch Sunrise-sunset API once a day.**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-03-27 | 2026-03-27 | 1.0.3 | 4 |

- Mileage summary distance corrected.
- EV labels corrected.
- Sunrise and sunset are fetched once a day.

## 1.0.2 (3)

**Documentation, GIT Markdown files added. Exception handled for Sunrise-Sunset API, Code Cleanup.**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-03-18 | 2026-03-18 | 1.0.2 | 3 |

- Markdown docs added.
- A sunrise/sunset failure is handled instead of breaking the screen.

## 1.0.1 (2)

**Final Changes before Testing Release: Added Image Logo, Moved Autopay/Announcements to Home, Removed Comments, and minor fixes.**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-03-15 | 2026-03-15 | 1.0.1 | 2 |

- App logo added.
- AutoPay and announcements moved to Home.
- Prepared for the testing release.

## Expense, Mileage, Shenshai (incl Muhurat) code and UI revamping plus logic fixes, NevAPI link with Feedback and Issue Logs, Announcement added.

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-03-13 |  |  |  |

- Expense, Mileage, and Shenshai, including Muhurat, revamped.
- NevAPI connected for feedback and issue logs.
- Announcements added.

## 1.0.0 (1)

**First Commit Post Release: Includes Expense Tracker, Mileage Tracker, Shenshai Fixes. New Features: Support, Error Logging, API Calling, & Setting Configuration.**

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-03-05 | 2026-03-05 | 1.0.0 | 1 |

- Expense, Mileage, and Shenshai fixes.
- Support, error logging, API calls, and settings configuration added.

## Added AutoPay, Shenshai, Admin and fixes.

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-02-09 |  |  |  |

- AutoPay added.
- Shenshai added.
- Admin mode and fixes.

## Refactoring & Fixes.

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-01-28 |  |  |  |

- Refactoring and fixes. No store build.

## Settings Page Added.

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-01-27 |  |  |  |

- Settings page added.

## Google Places API and Code Refactoring wrt MileageTracker.

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-01-23 |  |  |  |

- Google Places used for Mileage.
- Mileage Tracker code refactored.

## MileageTracker CRUD Done.

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-01-20 |  |  |  |

- Mileage Tracker create, update, and delete completed.

## ExpenseTracker CRUD Done.

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-01-19 |  |  |  |

- Expense Tracker create, update, and delete completed.

## Add project files.

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-01-12 |  |  |  |

- Project files added.

## Add .gitattributes and .gitignore.

| Commit date | Release date | Version | Build |
| --- | --- | --- | --- |
| 2026-01-12 |  |  |  |

- Git attributes and gitignore added.
