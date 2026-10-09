# TMPS Labs — Consumed Media Watchlist

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)  
**Author:** Dinu Pleșu FAF241  
**Language:** C# (.NET 8)

## About

A small console watchlist for films, shows, books, games, and other media.
Create categories and tags, add and edit titles, mark items as consumed, and
rate them. Categories, tags, and media entries have IDs; assign categories and
tags by ID or name. New names are created automatically, and name matching
ignores letter case.
Lab 0 focuses on three SOLID principles without a database or UI framework.

| Lab | Branch | Topic |
|---|---|---|
| Lab 0 | `Lab-0` | Project setup + SRP, OCP, and DIP |

## Lab 0 — SOLID principles

The console project demonstrates SRP by separating media rules, library
storage, use cases, scoring, and input/output. OCP is demonstrated with the
replaceable `IMediaScoreCalculator` policy. DIP is demonstrated by
`WatchlistService` depending on the `IMediaLibrary` and
`IMediaScoreCalculator` abstractions.

### 1. Single Responsibility Principle (SRP)

> A class should have only one reason to change.

The classes divide the watchlist's responsibilities:

| Class | Its responsibility |
|---|---|
| `MediaItem` | Hold a title, category, tags, consumed state, and rating; enforce its rating rules |
| `MediaLabel` | Represent a category or tag with its own ID and name |
| `InMemoryMediaLibrary` | Store categories, tags, and media items in memory |
| `WatchlistService` | Coordinate watchlist operations using the library and scoring policy |
| `RatingScoreCalculator` | Convert a 1–10 rating into a five-star score |
| `WatchlistConsoleApp` | Read commands and display results in the console |

If the score formula changes, only the score policy needs to change. If the
console prompts change, the data storage and media rules remain separate.

### 2. Open/Closed Principle (OCP)

> Software entities should be open for extension but closed for modification.

Scoring uses the `IMediaScoreCalculator` abstraction:

```csharp
public interface IMediaScoreCalculator
{
    int? Calculate(int? rating);
}
```

The current `RatingScoreCalculator` divides a 1–10 rating by two, producing
half-star values from 0.5 to 5. To add another scoring policy, create a class
implementing `IMediaScoreCalculator` and select it in `Program.cs`;
`WatchlistService` can use it without being modified.

### 3. Dependency Inversion Principle (DIP)

> High-level modules should not depend on low-level modules; both should
> depend on abstractions.

`WatchlistService` depends on `IMediaLibrary` and
`IMediaScoreCalculator`, not on concrete storage or scoring classes:

```csharp
public sealed class WatchlistService(
    IMediaLibrary library,
    IMediaScoreCalculator scoreCalculator)
```

`Program.cs` connects the concrete implementations at startup:

```csharp
var library = new InMemoryMediaLibrary();
var scoreCalculator = new RatingScoreCalculator();
var watchlist = new WatchlistService(library, scoreCalculator);
```

This means the service can work with a different library or score policy
without depending directly on those implementations.

## Running the project

Requires the .NET 8 SDK.

```powershell
dotnet run --project .\Lab0.csproj
```

Enter `help` for commands. `add` and `edit` accept category/tag names or IDs
(for example, `1` or `#1`). Use `edit` to modify an item's title, category,
or tags; leave a field blank to keep its current value, or enter `-` to clear
its tags. New names are created automatically; matching ignores case. The app
stores data in memory, so it resets after exit. See
[DOCUMENTATION.md](./DOCUMENTATION.md) for the detailed class mapping and
workflow.
