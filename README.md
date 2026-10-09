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

## Lab 0 design

The console project demonstrates **SRP** by separating media rules, library
storage, use cases, scoring, and input/output. **OCP** is demonstrated with
the replaceable `IMediaScoreCalculator` policy. **DIP** is demonstrated by
`WatchlistService` depending on the `IMediaLibrary` and
`IMediaScoreCalculator` abstractions.

Requires the .NET 8 SDK:

```powershell
dotnet run --project .\Lab0\Lab0.csproj
```

See [Lab0/README.md](./Lab0/README.md) and
[Lab0/DOCUMENTATION.md](./Lab0/DOCUMENTATION.md).

The previous web/PostgreSQL implementation is preserved only in the local
`archive/` folder. Git ignores that folder so the larger experiment is not
included in the Lab 0 submission.
