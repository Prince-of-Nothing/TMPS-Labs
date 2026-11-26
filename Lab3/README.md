# Lab 3 — Behavioral Design Patterns

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)  
**Author:** Dinu Pleșu, FAF241  
**Technology:** C# / .NET 10 MAUI (Android)  
**Instructor:** Istrati Daniel

## What it is

The final watchlist app. It builds directly on Lab 2 (same `Watchlist.Core`
project and the same Markdown vault), adds a visual cover picker, undo/redo,
live statistics, a guarded entry lifecycle, search, filters/sorting and
AniList / MAL / CSV import. Settings switch the theme and the cover search
engine, and entries can use a custom kind and an existing category picked from
a list.

The vault is a folder of Markdown files. On first launch you choose where it
lives, and you can move it later from Settings → Library location. Moving copies
your entries and covers into the new folder without overwriting anything; the
old copy is kept.

The app has three bottom tabs, in the style of Aniyomi: **Library** (the list
or cover grid), **Activity** (a feed of recent changes) and **Settings**.

Settings covers:

- **Appearance:** theme mode (System, Light, Dark), an AMOLED black option for
  dark mode, and an accent color that recolors buttons and switches.
- **Library:** the default view (list or grid), the default sort, and whether
  deleting asks first.
- **Cover search engine** and **Library location** (move the vault to app
  storage, shared Documents or a custom folder; see below).

Colors come from one palette (`Appearance.cs`), so light, dark and AMOLED
themes all update without restarting the app.

```text
/storage/emulated/0/Documents/TMPS-Watchlist/   shared, visible in Files
  index.md
  entries/0001-the-matrix.md
  covers/0001.jpg
```

- **Shared Documents** asks for Android’s “All files access” permission, then
  creates `Documents/TMPS-Watchlist`. It is visible in the Files app and any
  text editor.
- **App storage** uses `FileSystem.AppDataDirectory/library` and needs no
  permission, but is private to the app.
- Existing app-private entries/covers are copied into the chosen folder without
  overwriting files already there.
- The choice is remembered. If the folder becomes unavailable or the permission
  is revoked, the setup screen appears again.
- `TMPS_LAB3_LIBRARY` overrides the location for desktop/testing.

`MANAGE_EXTERNAL_STORAGE` is fine for a sideloaded lab APK; Google Play
restricts that permission for general apps. A production app would use the
Storage Access Framework folder picker instead.

## Project layout

```text
Lab3/
  Behavioral/              Watchlist.Behavioral (net10.0)
    Commands/              receiver, command invoker, add/edit/delete/lifecycle/import
    Observers/             subject, statistics, activity log, UI callback
    States/                Planned, Consumed, Rated states + context
    Export/                Strategy: CSV and Markdown
    Import/                AniList GraphQL + MAL XML + CSV adapters
  App/                     Lab3 MAUI Android client
                           package net.plesu.dinu.shameless.watchlist
    MainPage               cards or cover grid, stats, search, filters, undo/redo, export
    ActivityPage           activity feed tab
    EditPage               metadata, custom kind, category picker
    CoverPickerPage        provider search + local download
    ImportPage             source preview + import with progress
    SettingsPage           appearance, library defaults, cover engine, location
    StorageSetupPage       first-run folder + permission choice
    StorageLocations       shared/app paths + permission flow
    Appearance.cs          preferences and the light/dark/AMOLED palette
    Resources/AppIcon      adaptive app icon (SVG background + foreground)
    Resources/Images       logo.svg shown in the header
```

## Behavioral patterns

- **Command** — `IWatchlistCommand` + `CommandInvoker` with undo/redo.
  `AddMediaCommand`, `EditMediaCommand`, `DeleteMediaCommand`,
  `ConsumeMediaCommand`, `RateMediaCommand`, `ReopenMediaCommand`,
  `SetCoverCommand`, `ImportMediaCommand`. The receiver
  (`WatchlistEditor`) owns the business calls; commands carry parameters and
  inverse operations.
- **Observer** — `WatchlistEvents` is the subject, `IWatchlistObserver` the
  interface. `StatisticsObserver` recomputes counts/average,
  `ActivityLogObserver` keeps the feed, and `DelegateObserver` refreshes the
  UI. Observers subscribe/unsubscribe at runtime; a failing observer cannot
  block a save.
- **State** — `MediaLifecycle` is the context, `IMediaState` the interface,
  and `PlannedState`, `ConsumedState`, `RatedState` the concrete states.
  Planned rejects rating, consumed accepts it, rated can be reopened; undo
  restores the exact previous snapshot.
- **Strategy** — `IWatchlistExporter` with `CsvWatchlistExporter` and
  `MarkdownWatchlistExporter`; `ExportService.Use` swaps them at runtime.

Patterns interact: a UI Command mutates the repository, the change is
published to Observers, lifecycle Commands pass through State, and the export
Strategy is selected at runtime. Lab 2's cover Adapter/Decorator/Proxy are
reused by the cover picker and import path.

## Import and export

- **AniList** — public username, GraphQL, no token. Maps anime/manga, status,
  POINT_10 score, progress, format, genres and cover URL.
- **MAL** — reads the XML from Profile → Export/Import. No OAuth. Maps anime
  and manga rows, Completed/score, progress and cover URL.
- **CSV** — any service that exports a title list. `Title` is required;
  `Kind`, `Category`, `Status`, `Rating`, `Progress` and `Tags` are optional.
- Duplicates are matched by normalized title + kind and shown in the preview.
  Import runs as one undoable Command with a live progress bar; a failure rolls
  back all entries from that run.
- **Export** — CSV or Markdown, written to the cache folder and opened with the
  Android share sheet.

## Build and run

Requirements: .NET 10 SDK, MAUI Android workload, JDK 21, Android SDK.

```powershell
dotnet workload install maui-android
$env:JAVA_HOME = "C:\path\to\jdk-21"
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
dotnet build .\Lab3\App\Lab3.csproj -f net10.0-android
```

APK output: `Lab3/App/bin/Debug/net10.0-android/net.plesu.dinu.shameless.watchlist-Signed.apk`.

Pushing a `labs-v*` tag builds the release APK in CI and attaches it to the
GitHub Release (the workflow also publishes the Lab 2 APK and both
reports):

```powershell
git tag labs-v1.0
git push origin labs-v1.0
```

## Tests

```powershell
dotnet test .\Tests\Watchlist.Tests\Watchlist.Tests.csproj
```

The suite covers command undo/redo, observer subscription and failure
isolation, all state transitions, strategy switching, AniList/MAL/CSV mapping,
duplicate detection, import undo, custom-kind persistence and vault migration.

Full report: [`../Reports/Lab3.md`](../Reports/Lab3.md).
