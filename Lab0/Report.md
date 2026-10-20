# Laboratory Work No. 0

## Consumed Media Watchlist — SOLID Design Principles

| | |
|---|---|
| **Discipline** | Software Design Techniques and Mechanisms (TMPS) |
| **Author** | Dinu Pleșu, FAF241 |
| **Technology** | C# / .NET 8 |
| **Application type** | Console application |
| **Year** | 2026 |

## Abstract

This laboratory work presents a small console application for managing
planned and consumed media. Users can organize media items with categories
and tags, mark items as consumed, and assign ratings. The implementation
demonstrates the Single Responsibility Principle (SRP), Open/Closed Principle
(OCP), and Dependency Inversion Principle (DIP) through separated
responsibilities, an interchangeable scoring abstraction, and
interface-based dependencies.

## 1. Project theme

The theme is a personal consumed-media watchlist. The application can
represent films, television shows, books, games, and other media as
watchlist entries. Each entry has an ID, title, category, optional tags,
consumption status, and optional rating. Categories and tags also have IDs
and can be selected by ID or name.

The application is intentionally limited to a console and in-memory storage.
Its purpose is to demonstrate the chosen design principles without
introducing database, network, or graphical-interface concerns.

## 2. Objective and requirements

The objective is to implement core watchlist operations while keeping the
application understandable and open to selected changes.

The application supports:

- Creating or reusing categories and tags.
- Adding and editing media items.
- Assigning categories and tags by name or ID.
- Marking an item as consumed.
- Rating consumed media from 1 to 10.
- Displaying items, their status, and their score.

Names are matched without regard to letter case. A category or tag that
does not yet exist is created when entered by name. Ratings are only accepted
after an item has been marked as consumed. Data is not retained after the
application exits.

## 3. Technologies and project structure

The application is written in C# and targets .NET 8.

| File/type | Responsibility |
|---|---|
| `Program.cs` | Creates and connects the application components |
| `MediaItem.cs` / `MediaItem` | Stores media state and enforces consumption and rating rules |
| `MediaLabel.cs` / `MediaLabel` | Represents a category or tag |
| `IMediaLibrary.cs` / `IMediaLibrary` | Defines the storage operations used by the service |
| `InMemoryMediaLibrary.cs` / `InMemoryMediaLibrary` | Stores media and labels in memory |
| `IMediaScoreCalculator.cs` / `IMediaScoreCalculator` | Defines the score-calculation policy |
| `RatingScoreCalculator.cs` / `RatingScoreCalculator` | Converts a rating to a five-star score |
| `WatchlistService.cs` / `WatchlistService` | Coordinates watchlist operations |
| `WatchlistConsoleApp.cs` / `WatchlistConsoleApp` | Handles command-line input and output |

## 4. Application workflow

At startup, `Program.cs` creates the in-memory library and score calculator,
passes them to `WatchlistService`, and then starts the console application.
The user can create labels or enter new category/tag names while adding or
editing an item. The service validates required text and delegates storage
and lookup to the library.

To rate an item, the user first marks it as consumed. `MediaItem.Rate`
enforces that rule and restricts ratings to the range 1–10. When the
watchlist is displayed, `RatingScoreCalculator` divides the rating by two;
for example, a rating of 9 is displayed as 4.5 out of 5 stars.

## 5. SOLID principles demonstrated in code

Lab 0 specifically demonstrates SRP, OCP, and DIP. The examples below are
from the implementation and show where each principle appears.

### 5.1 Single Responsibility Principle (SRP)

> A class should have one responsibility and one reason to change.

The code places separate concerns in separate types:

- `MediaItem` owns an item's state and its consumption/rating rules.
- `InMemoryMediaLibrary` owns in-memory collections and label/item lookup.
- `WatchlistService` coordinates application operations.
- `RatingScoreCalculator` owns the conversion from a rating to a score.
- `WatchlistConsoleApp` owns console prompts, command dispatch, and display.

For example, rating validity is enforced by the media entity rather than by
the console:

```csharp
public void Rate(int rating)
{
    if (!IsConsumed)
    {
        throw new InvalidOperationException("Mark the media as consumed before rating it.");
    }

    if (rating is < 1 or > 10)
    {
        throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be from 1 to 10.");
    }

    Rating = rating;
}
```

This keeps a business rule in the type that owns the corresponding media
state. The console handles user interaction, while the entity protects its
own state regardless of which caller invokes it.

### 5.2 Open/Closed Principle (OCP)

> Software entities should be open for extension but closed for modification.

The scoring policy is represented by an interface:

```csharp
public interface IMediaScoreCalculator
{
    decimal? Calculate(int? rating);
}
```

The current implementation supplies one scoring rule:

```csharp
public sealed class RatingScoreCalculator : IMediaScoreCalculator
{
    public decimal? Calculate(int? rating) => rating / 2m;
}
```

`WatchlistService` calls the abstraction rather than embedding that formula:

```csharp
public decimal? GetScore(MediaItem item) => scoreCalculator.Calculate(item.Rating);
```

A new scoring policy can implement `IMediaScoreCalculator` and be selected
at startup without modifying the service. The present implementation uses a
five-star scale with half-star increments.

### 5.3 Dependency Inversion Principle (DIP)

> High-level modules should not depend on low-level modules; both should
> depend on abstractions.

`WatchlistService` receives abstractions instead of constructing storage and
scoring implementations itself:

```csharp
public sealed class WatchlistService(
    IMediaLibrary library,
    IMediaScoreCalculator scoreCalculator)
```

Concrete components are connected at the application entry point:

```csharp
var library = new InMemoryMediaLibrary();
var scoreCalculator = new RatingScoreCalculator();
var watchlist = new WatchlistService(library, scoreCalculator);
```

The service can therefore use another `IMediaLibrary` or
`IMediaScoreCalculator` implementation without changing its own operation
logic. `Program.cs` is the composition root: it selects and connects the
concrete implementations.

### 5.4 Principles outside this lab's stated scope

This work focuses on SRP, OCP, and DIP. It does not claim a separate,
intentional demonstration of all five SOLID principles. In particular,
Liskov Substitution Principle (LSP) and Interface Segregation Principle (ISP)
are not presented as lab objectives.

## 6. Running and using the application

With the .NET 8 SDK installed, run:

```powershell
dotnet run --project .\Lab0.csproj
```

The `help` command lists the available commands: `category`, `tag`, `add`,
`edit`, `consumed`, `rate`, `list`, `help`, and `quit`. During editing, a
blank title or category keeps its current value; a blank tags input also
keeps the existing tags, while `-` removes all tags.

## 7. Results and limitations

The result is a working console watchlist with separate types for the
domain, storage, service operations, score calculation, and user interface.
The score-calculation abstraction allows another policy to be selected
without coupling the service to a formula, and the storage abstraction
keeps the service independent of the in-memory implementation.

The implementation is intentionally small. It does not persist data,
provide a graphical interface, or connect to an external service. These
features could be added later, for example by implementing another
`IMediaLibrary` for persistent storage.

## 8. Conclusion

The laboratory work implements the core workflow of a consumed-media
watchlist and uses it to demonstrate three SOLID principles in concrete
code. SRP is reflected in the separation of domain rules, storage,
coordination, scoring, and user interaction. OCP appears in the replaceable
score-calculation policy, and DIP appears in the service's interface-based
dependencies and the composition root.

The resulting design is small enough to follow while still allowing
individual responsibilities and policies to evolve independently.
