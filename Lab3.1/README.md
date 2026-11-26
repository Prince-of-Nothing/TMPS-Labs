# Lab 3.1 — Behavioral patterns, one folder per pattern

Lab 3.1 is the same application as [Lab 3](../Lab3/README.md), with the same behaviour and the same tests.
Only the folder layout changes, so each pattern can be found by its name.

## Where each pattern lives

| Pattern | Folder | Main types |
|---|---|---|
| Command (with undo/redo) | `Behavioral/Patterns/Command/` | `IWatchlistCommand`, `CommandInvoker` (invoker), `LifecycleCommand`, `RateMediaCommand`, `ConsumeMediaCommand`, `ReopenMediaCommand`, `AddMediaCommand`, `DeleteMediaCommand`, `EditMediaCommand`, `ImportMediaCommand`, `SetCoverCommand`, `WatchlistEditor` (receiver) |
| Observer | `Behavioral/Patterns/Observer/` | `IWatchlistObserver`, `WatchlistEvents` (subject), `ActivityLogObserver`, `StatisticsObserver`, `DelegateObserver`, `WatchlistChange` |
| State | `Behavioral/Patterns/State/` | `MediaLifecycle` (context), `IMediaState`, `PlannedState`, `ConsumedState`, `RatedState` |
| Strategy (export) | `Behavioral/Patterns/Strategy/Export/` | `ExportService` (context), `IWatchlistExporter`, `CsvWatchlistExporter`, `MarkdownWatchlistExporter` |
| Strategy (import) | `Behavioral/Patterns/Strategy/Import/` | `IWatchlistImporter`, `AniListImporter`, `MalXmlImporter`, `CsvWatchlistImporter`, `ImportService` |

Each folder holds one pattern. The role names (receiver, invoker, subject, context, strategy) are written in the class summaries.

## Layout

```text
Lab3.1/
├── Behavioral/                       pattern library (no UI code)
│   └── Patterns/
│       ├── Command/
│       ├── Observer/
│       ├── State/
│       └── Strategy/
│           ├── Export/
│           └── Import/
└── App/                              .NET MAUI Android client (Watchlist Lab 3.1)
Tests/Watchlist.Tests31/              unit tests for Lab 3.1
```

The app's package id is `net.plesu.dinu.shameless.watchlist31`, so it installs next to Lab 3.

## Build and test

```powershell
dotnet test Tests/Watchlist.Tests31/Watchlist.Tests.csproj
dotnet build Lab3.1/App/Lab3.csproj -f net10.0-android
```

Android builds need JDK 21 (see [Lab 3 report](../Reports/Lab3.md) §13).
