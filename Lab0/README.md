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
| Lab 1+ | `Lab-N` | Creational, structural, and behavioral design patterns |

## SOLID in this project

- **SRP:** the media item, library, watchlist service, score calculator, and
  console app have separate responsibilities.
- **OCP:** `IMediaScoreCalculator` lets you add a score policy without changing
  `WatchlistService`.
- **DIP:** `WatchlistService` depends on `IMediaLibrary` and
  `IMediaScoreCalculator`, not the concrete implementations.

## Run

Requires the .NET 8 SDK.

```powershell
dotnet run --project .\Lab0.csproj
```

Enter `help` for commands. Use `edit` to modify an item's title, category, or
tags; leave a field blank to keep its current value, or enter `-` to clear its
tags. The `add` and `edit` commands accept category/tag names or IDs (for
example, `1` or `#1`). The app stores data in memory, so it resets after exit. See
[DOCUMENTATION.md](./DOCUMENTATION.md) for the class mapping and workflow.

The earlier PostgreSQL/web experiment is retained locally at
`archive/lab0-web-postgres/` and is ignored by Git.
