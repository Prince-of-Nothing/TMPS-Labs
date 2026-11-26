# Lab 3 — Behavioral Design Patterns

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)  
**Author:** Dinu Pleșu, FAF241  
**Technology:** C# / .NET 10 MAUI (Android)  
**Instructor:** Istrati Daniel

## What it is

The final watchlist app. It builds directly on Lab 2 (same `Watchlist.Core`
project and the same Markdown vault), adds a visual cover picker, undo/redo,
live statistics, a guarded entry lifecycle, filters/sorting and AniList / MAL
import.

The vault is a folder of Markdown files. On first launch you choose where it
lives:

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
    Import/                AniList GraphQL + MAL XML adapters
  App/                     Lab3 MAUI Android client
    MainPage               cards, stats, filters, undo/redo, export
    EditPage               metadata + cover entry point
    CoverPickerPage        provider search + local download
    ImportPage             AniList username / MAL XML preview + import
    StorageSetupPage       first-run folder + permission choice
    StorageLocations       shared/app paths + permission flow
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
- Duplicates are matched by normalized title + kind and shown in the preview.
  Import runs as one undoable Command.
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

APK output: `Lab3/App/bin/Debug/net10.0-android/com.tmps.lab3.watchlist-Signed.apk`.

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
isolation, all state transitions, strategy switching, AniList/MAL mapping,
duplicate detection, import undo and vault migration.

Full report: [`../Reports/Lab3.md`](../Reports/Lab3.md).
