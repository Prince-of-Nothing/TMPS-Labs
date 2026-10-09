# Lab 0 — Consumed Media Watchlist

## Concept

A small console watchlist for films, shows, books, games, and other media.
Create media categories and reusable tags, add titles you want to experience,
mark titles as consumed, and rate consumed items from 1 to 10. The displayed
score is the rating divided by two, shown on a five-star scale with half-star
increments. Existing entries can be edited to
change their title, category, or tags. Categories, tags, and media items have
IDs. You can assign a category or tag by ID or name; names match without
considering letter case, and a name that does not exist is created automatically.

This Lab 0 version keeps everything in memory and has no GUI, database, or
network connection. Its purpose is to make the three design principles easy
to see before adding features in later labs.

## The three SOLID principles

### Single Responsibility Principle (SRP)

> A class should have only one reason to change.

- `MediaItem` holds one title and enforces its consumed/rating rules.
- `InMemoryMediaLibrary` keeps categories, tags, and media items.
- `WatchlistService` coordinates watchlist actions.
- `RatingScoreCalculator` converts a 1–10 rating into a five-star score.
- `WatchlistConsoleApp` handles the command-line interaction.
- `Program.cs` connects the pieces and starts the application.

### Open/Closed Principle (OCP)

> Software entities should be open for extension but closed for modification.

`WatchlistService` calculates scores through `IMediaScoreCalculator`. The
current `RatingScoreCalculator` divides a 1–10 rating by two, producing
half-star values from 0.5 to 5. A different score policy can implement that
interface and be selected in `Program.cs` without changing the service.

### Dependency Inversion Principle (DIP)

> High-level modules should not depend on low-level modules; both should
> depend on abstractions.

`WatchlistService` depends on the `IMediaLibrary` and
`IMediaScoreCalculator` interfaces rather than the in-memory library and
concrete scoring class. The concrete implementations are chosen at startup
in `Program.cs`.

## Project structure

```text
Lab0/
├── Program.cs                   composition root
├── MediaItem.cs                 media data and rating rules
├── MediaLabel.cs                ID and name for a category or tag
├── IMediaLibrary.cs             library abstraction (DIP)
├── InMemoryMediaLibrary.cs      local in-memory data
├── IMediaScoreCalculator.cs     replaceable scoring policy (OCP)
├── RatingScoreCalculator.cs    score implementation
├── WatchlistService.cs         watchlist operations (SRP)
└── WatchlistConsoleApp.cs       command-line interface
```

## Run

Requires the .NET 8 SDK:

```powershell
dotnet run --project .\Lab0.csproj
```

Commands: `category`, `tag`, `add`, `edit`, `consumed`, `rate`, `list`, `help`,
and `quit`. The list shows IDs for media, categories, and tags. For example,
enter `1` or `#1` to assign category/tag ID 1, or enter a name such as `Web
Novel`. If that name is new, it is created; matching existing names ignores
letter case (`web novel` reuses `Web Novel`). Category and tag IDs are separate.

Use `edit` to change an item's title, category, or tags. During edit, leave a
field blank to keep its current value, or enter `-` to remove all tags.
Consumption status and ratings are managed with the separate `consumed` and
`rate` commands. Data is cleared when the app exits.
