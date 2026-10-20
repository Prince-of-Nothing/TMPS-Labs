# Lab 0 — Consumed Media Watchlist

| | |
|---|---|
| **Course** | Tehnici și Mecanisme de Proiectare Software (TMPS) |
| **Author** | Dinu Pleșu, FAF241 |
| **Technology** | C# and .NET 8 |
| **Application type** | Console application |

## Project theme

The project is a personal catalog for media that a user plans to experience
or has already consumed. It supports films, shows, books, games, and other
media through a shared set of categories and tags. Lab 0 keeps the application
small so its design and the selected SOLID principles are easy to follow.

## Goals and scope

The lab demonstrates how to separate responsibilities, make a policy
replaceable, and keep application logic dependent on abstractions. It focuses
on the Single Responsibility Principle (SRP), Open/Closed Principle (OCP),
and Dependency Inversion Principle (DIP).

The application can:

- Create or reuse categories and tags.
- Add and edit media items with a title, category, and optional tags.
- Mark an item as consumed and rate it from 1 to 10.
- Display the watchlist, status, rating, and calculated five-star score.
- Refer to media items, categories, and tags by ID; category and tag IDs are
  separate.

Categories and tags can be entered by name or ID. New names are created
automatically, and matching ignores letter case. Data is held in memory and
is cleared when the application exits.

## Architecture and SOLID in the code

| Type | Role | Principle connection |
|---|---|---|
| `MediaItem` | Holds media state and enforces consumption/rating rules | SRP |
| `MediaLabel` | Represents a category or tag with an ID and name | SRP |
| `InMemoryMediaLibrary` | Stores items, categories, and tags | SRP; implements `IMediaLibrary` |
| `WatchlistService` | Coordinates watchlist operations | SRP; depends on abstractions (DIP) |
| `RatingScoreCalculator` | Converts a rating to a five-star score | SRP; one implementation of the OCP extension point |
| `WatchlistConsoleApp` | Reads commands and writes console output | SRP |
| `Program.cs` | Creates and connects concrete application components | Composition root for DIP |

### Single Responsibility Principle (SRP)

> A class should have one responsibility and one reason to change.

The console UI, use-case coordination, in-memory storage, media rules, and
score calculation are kept in separate types. For example, changing the
score formula belongs in the calculator, while changing command prompts
belongs in the console app.

### Open/Closed Principle (OCP)

> Software entities should be open for extension but closed for modification.

`WatchlistService` calculates scores through `IMediaScoreCalculator`, rather
than containing a specific formula. A different policy can implement the
interface and be selected at startup without editing the service:

```csharp
public interface IMediaScoreCalculator
{
    decimal? Calculate(int? rating);
}
```

`RatingScoreCalculator` currently divides a rating from 1 to 10 by two, so
the resulting score is between 0.5 and 5, in half-star increments.

### Dependency Inversion Principle (DIP)

> High-level modules should not depend on low-level modules; both should
> depend on abstractions.

The service receives interfaces in its primary constructor:

```csharp
public sealed class WatchlistService(
    IMediaLibrary library,
    IMediaScoreCalculator scoreCalculator)
```

The application entry point supplies the concrete implementations:

```csharp
var library = new InMemoryMediaLibrary();
var scoreCalculator = new RatingScoreCalculator();
var watchlist = new WatchlistService(library, scoreCalculator);
```

This keeps watchlist operations independent of the particular storage and
score-calculation implementations. Lab 0 focuses on SRP, OCP, and DIP; it
does not claim to demonstrate every SOLID principle.

## Typical workflow

1. Start the app and enter `help` to see the available commands.
2. Use `category` and `tag` to create reusable labels, or enter new names
   while adding media.
3. Use `add` to create a planned media item.
4. Use `list` to see IDs, categories, tags, status, and score.
5. Use `edit` to change a title, category, or tags. A blank field keeps its
   current value; entering `-` for tags removes all tags.
6. Use `consumed` before `rate`; a rating can only be assigned to consumed
   media.

## Commands

| Command | Purpose |
|---|---|
| `category` | Create or reuse a category |
| `tag` | Create or reuse a tag |
| `add` | Add a media item |
| `edit` | Change an item's title, category, or tags |
| `consumed` | Mark an item as consumed |
| `rate` | Rate consumed media from 1 to 10 |
| `list` | Display the watchlist |
| `help` | Show command help |
| `quit` | Exit the app |

## Run

Requires the .NET 8 SDK:

```powershell
dotnet run --project .\Lab0.csproj
```

## Limitations and possible extensions

This lab intentionally has no database, graphical interface, network
connection, or persistence between runs. Possible later extensions include
persistent storage behind another `IMediaLibrary` implementation, additional
score policies, automated tests, and richer media-specific details.
