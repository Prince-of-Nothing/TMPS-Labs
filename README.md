# TMPS Labs — Consumed Media Watchlist

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)  
**Author:** Dinu Pleșu FAF241  
**Language:** C# (.NET 8)

## About the project

A semester project about a personal watchlist for media you plan to consume
or have already consumed. Lab 0 is intentionally small: create categories and
tags, add and edit media, mark it as consumed, and rate it. Media, categories,
and tags have IDs. Assign categories and tags by ID or name; new names are
created automatically, and matching ignores letter case.

| Lab | Branch | Topic |
|---|---|---|
| Lab 0 | `Lab-0` | Project setup + three SOLID principles (SRP, OCP, DIP) |
| Lab 1+ | `Lab-N` | Creational, structural, and behavioral design patterns |

## Lab 0 — SOLID principles

The console project demonstrates SRP by separating media rules, library
storage, use cases, scoring, and input/output. OCP is demonstrated with the
replaceable `IMediaScoreCalculator` policy. DIP is demonstrated by
`WatchlistService` depending on the `IMediaLibrary` and
`IMediaScoreCalculator` abstractions.

Lab 0 is a console-based consumed-media watchlist. It lets you create media
categories and tags, add and edit titles, mark items as consumed, and rate
consumed items from 1 to 10. Each media item, category, and tag has an ID.
When assigning categories or tags, you can enter the ID or name. New names are
created automatically, and matching ignores letter case (for example,
`mANGA` reuses `Manga`).

### 1. Single Responsibility Principle (SRP)

> A class should have only one reason to change.

The watchlist separates data, storage, business operations, scoring, and
console interaction:

| Class | Its responsibility |
|---|---|
| `MediaItem` | Hold a title, category, tags, consumed state, and rating; enforce its rating rules |
| `MediaLabel` | Represent a category or tag with its own ID and name |
| `InMemoryMediaLibrary` | Store categories, tags, and media items in memory |
| `WatchlistService` | Coordinate watchlist operations using the library and scoring policy |
| `RatingScoreCalculator` | Convert a 1–10 rating into a five-star score |
| `WatchlistConsoleApp` | Read commands and display results in the console |

For example, changing how scores are calculated does not require changing
`MediaItem` or the console. Changing the command-line prompts does not require
changing how media is stored.

### 2. Open/Closed Principle (OCP)

> Software entities should be open for extension but closed for modification.

Scoring is defined by the `IMediaScoreCalculator` interface:

```csharp
public interface IMediaScoreCalculator
{
    int? Calculate(int? rating);
}
```

`WatchlistService` uses this abstraction to calculate a score. The current
`RatingScoreCalculator` divides a rating of 1–10 by two, producing half-star
values from 0.5 to 5. To try a different scoring rule, add another
implementation of `IMediaScoreCalculator` and select it in `Program.cs`; the
watchlist service does not need to change.

### 3. Dependency Inversion Principle (DIP)

> High-level modules should not depend on low-level modules; both should
> depend on abstractions.

`WatchlistService` receives abstractions rather than constructing concrete
storage and scoring implementations itself:

```csharp
public sealed class WatchlistService(
    IMediaLibrary library,
    IMediaScoreCalculator scoreCalculator)
```

`IMediaLibrary` represents the operations the service needs from storage, and
`IMediaScoreCalculator` represents its scoring policy. The concrete
implementations are selected at the composition root in `Program.cs`:

```csharp
var library = new InMemoryMediaLibrary();
var scoreCalculator = new RatingScoreCalculator();
var watchlist = new WatchlistService(library, scoreCalculator);
```

This keeps the watchlist operations independent of the in-memory storage and
the particular scoring formula.

### Running Lab 0

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet run --project .\Lab0\Lab0.csproj
```

Type `help` in the app to see the commands. `add` and `edit` accept category
and tag IDs or names; entering a new name creates it. The data is held in
memory and is cleared when the app exits.

See [Lab0/README.md](./Lab0/README.md) and
[Lab0/DOCUMENTATION.md](./Lab0/DOCUMENTATION.md).

## What comes next

Later labs can extend this watchlist with design patterns while keeping its
media, category, and tag concepts: for example, Strategy for alternative
scoring policies, Factory for creating media types, or Observer for reacting
to watchlist changes.
