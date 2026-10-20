# Lab 0 Project Report — Consumed Media Watchlist

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)  
**Author:** Dinu Pleșu, FAF241  
**Technology:** C# and .NET 8

## 1. Project overview

The Consumed Media Watchlist is a console application for tracking films,
shows, books, games, and other media. Users can add and edit entries, organize
them with categories and tags, mark them as consumed, and rate consumed media
from 1 to 10. The displayed score is calculated from the rating on a 100-point
scale.

The application persists its data as JSON. By default, the file is
`%LOCALAPPDATA%\TMPS-Labs\Lab0\watchlist.json`. The
`TMPS_LAB0_WATCHLIST_PATH` environment variable can specify a different file.
Media, categories, tags, consumption status, and ratings are restored when the
application starts.

## 2. Application structure

The application separates user interaction, business operations, data storage,
domain rules, and score calculation:

| Type | Responsibility |
|---|---|
| `Program.cs` | Composition root; creates the concrete components and starts the app |
| `WatchlistConsoleApp` | Reads commands and inputs, then displays results and errors |
| `WatchlistService` | Validates required values and coordinates watchlist operations |
| `IMediaLibrary` | Defines the storage operations used by the service |
| `JsonMediaLibrary` | Loads and saves the watchlist JSON file |
| `InMemoryMediaLibrary` | Manages media and label collections and assigns IDs |
| `MediaItem` | Holds media state and enforces consumption and rating rules |
| `MediaLabel` | Represents a category or tag with an ID and name |
| `IMediaScoreCalculator` | Defines the replaceable score-calculation policy |
| `RatingScoreCalculator` | Converts a 1–10 rating into a score out of 100 |

The main runtime flow is:

1. `Program.cs` creates `JsonMediaLibrary`; existing JSON data is loaded.
2. The console application reads a command and collects any required input.
3. `WatchlistService` validates the input and calls the library.
4. The library updates the in-memory collections and saves changes to JSON.
5. The console displays the result. For `list`, it asks the calculator for
   each item's score.

## 3. SOLID principles demonstrated

Lab 0 focuses on the Single Responsibility, Open/Closed, and Dependency
Inversion principles. It does not claim to demonstrate all five SOLID
principles.

### 3.1 Single Responsibility Principle (SRP)

> A class should have one primary responsibility and one reason to change.

The application separates responsibilities into different types. For example,
console prompt changes belong in `WatchlistConsoleApp`, persistence changes
belong in `JsonMediaLibrary`, media state rules belong in `MediaItem`, and
changes to the score formula belong in `RatingScoreCalculator`.

This separation keeps unrelated concerns from accumulating in one large class.
The classes can still collaborate to complete a user action, but each owns a
distinct part of that action.

### 3.2 Open/Closed Principle (OCP)

> Software should allow behavior to be extended without requiring existing
> high-level code to be modified for each new policy.

The score policy is represented by `IMediaScoreCalculator`. The current
`RatingScoreCalculator` implementation multiplies a rating by ten. A different
scoring policy can implement the same interface and be selected in `Program.cs`.
`WatchlistService` continues to call the interface, rather than containing a
specific scoring formula.

### 3.3 Dependency Inversion Principle (DIP)

> High-level modules should depend on abstractions rather than directly on
> low-level implementation details.

`WatchlistService` receives an `IMediaLibrary` and an
`IMediaScoreCalculator` through its constructor. It does not construct
`JsonMediaLibrary` or `RatingScoreCalculator` itself. `Program.cs` supplies
those concrete implementations at startup.

Because of this, the service can work with another storage implementation or
score calculator that satisfies the same interface.

## 4. User workflow

The application supports these console commands:

| Command | Purpose |
|---|---|
| `category` | Create or reuse a category |
| `tag` | Create or reuse a tag |
| `add` | Add a media item |
| `edit` | Change a title, category, or tags |
| `consumed` | Mark an item as consumed |
| `rate` | Rate consumed media from 1 to 10 |
| `list` | Display the saved watchlist and scores |
| `help` | Show the available commands |
| `quit` | Exit the application |

Categories and tags can be assigned by name or ID. Names are matched without
regard to letter case; entering a name that does not exist creates it. A media
item must be marked as consumed before it can be rated.

Run the application from the repository root with:

```powershell
dotnet run --project .\Lab0\Lab0.csproj
```

## 5. Scope and limitations

The project uses a local JSON file rather than a database. It is a single-user
console application and has no graphical interface or network service. JSON
loading validates the stored format version and reports invalid or unsupported
data rather than silently replacing it.

Possible extensions include another `IMediaLibrary` implementation backed by a
database, additional score-calculation policies, automated tests, and
media-specific fields.
