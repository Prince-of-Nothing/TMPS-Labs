# Lab 0 — Consumed Media Watchlist

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)  
**Author:** Dinu Pleșu, FAF241  
**Technology:** C# and .NET 8

## About

A small console watchlist for films, shows, books, games, and other media.
Create categories and tags, add and edit titles, mark items as consumed, and
rate them. Categories, tags, and media entries have IDs; assign categories and
tags by ID or name. New names are created automatically, and name matching
ignores letter case. Lab 0 focuses on three SOLID principles without a
database or UI framework.

| Lab | Branch | Topic |
|---|---|---|
| Lab 0 | `Lab-0` | Project setup + SRP, OCP, and DIP |
| Lab 1+ | `Lab-N` | Creational, structural, and behavioral design patterns |

## Goals and scope

The lab demonstrates how to separate responsibilities, make a policy
replaceable, and keep application logic dependent on abstractions. It focuses
on the Single Responsibility Principle (SRP), Open/Closed Principle (OCP),
and Dependency Inversion Principle (DIP).

The application can:

- Create or reuse categories and tags.
- Add and edit media items with a title, category, and optional tags.
- Mark an item as consumed and rate it from 1 to 10.
- Display the watchlist, status, rating, and calculated score out of 100.
- Refer to media items, categories, and tags by ID; category and tag IDs are
  separate.

Categories and tags can be entered by name or ID. New names are created
automatically, and matching ignores letter case. The watchlist is saved as
JSON at `%LOCALAPPDATA%\TMPS-Labs\Lab0\watchlist.json` and restored when the
application starts. Set `TMPS_LAB0_WATCHLIST_PATH` to use a different file.

## Architecture and SOLID in the code

| Type | Role | Principle connection |
|---|---|---|
| `MediaItem` | Holds media state and enforces consumption/rating rules | SRP |
| `MediaLabel` | Represents a category or tag with an ID and name | SRP |
| `JsonMediaLibrary` | Loads and saves items, categories, and tags as JSON | SRP; implements `IMediaLibrary` |
| `WatchlistService` | Coordinates watchlist operations | SRP; depends on abstractions (DIP) |
| `RatingScoreCalculator` | Converts a rating to a score out of 100 | SRP; one implementation of the OCP extension point |
| `WatchlistConsoleApp` | Reads commands and writes console output | SRP |
| `Program.cs` | Creates and connects concrete application components | Composition root for DIP |

### Single Responsibility Principle (SRP)

> A class should have only one reason to change.

The console UI, use-case coordination, JSON persistence, media rules, and
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
    int? Calculate(int? rating);
}
```

`RatingScoreCalculator` currently multiplies a 1–10 rating by ten, producing
a score from 10 to 100.

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
var library = new JsonMediaLibrary();
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
dotnet run --project .\Lab0\Lab0.csproj
```

## Limitations and possible extensions

This lab intentionally has no database, graphical interface, or network
connection. JSON persistence keeps the watchlist between runs. Possible later
extensions include a database-backed `IMediaLibrary` implementation,
additional score policies, automated tests, and richer media-specific details.

The earlier PostgreSQL/web experiment is retained locally at
`archive/lab0-web-postgres/` and is ignored by Git.