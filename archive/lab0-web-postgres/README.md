# TMPS Labs — Consumed Media Library

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)  
**Author:** Dinu Pleșu FAF241  
**Language:** C# (ASP.NET Core, .NET 8)  
**Database:** PostgreSQL

## About the project

Afterglow is a browser-based personal library for films, series, books, games,
and other media. Add things to experience, organize them with categories and
tags, mark them as consumed, and give them a score. You can also add your own
typed metadata fields — like custom columns in Calibre — from the app.

The app is designed to grow over the semester, with Lab 0 demonstrating
**SRP, OCP, and DIP** and later labs adding design patterns.

| Lab | Branch | Topic |
|---|---|---|
| Lab 0 | `Lab-0` | Project setup + 3 SOLID principles (SRP, OCP, DIP) |
| Lab 1+ | `Lab-N` | Creational, structural, and behavioral design patterns |

## Project structure

```text
Lab0/
├── Program.cs                              web host, API endpoints, dependency wiring
├── Abstractions/                           service, repository, and scoring contracts
├── Domain/                                 library and custom-field request/data models
├── Infrastructure/
│   └── PostgresWatchlistRepository.cs       PostgreSQL schema and persistence
├── Scoring/
│   └── RatingScoreCalculator.cs              replaceable score policy
├── Services/
│   └── WatchlistService.cs                   use-case validation and coordination
├── wwwroot/                                 browser UI
├── compose.yaml                             local PostgreSQL container
└── .env.example                             database configuration template
```

## Lab 0 — SOLID principles

- **SRP:** services coordinate use cases, the PostgreSQL repository handles
  persistence, the score calculator computes scores, and the browser UI
  handles presentation.
- **OCP:** `IMediaScoreCalculator` makes the rating-to-score policy
  replaceable without modifying `WatchlistService`.
- **DIP:** the service depends on `IWatchlistRepository` and
  `IMediaScoreCalculator`; the web endpoints use `IWatchlistService`.
  `Program.cs` wires concrete implementations.

The custom-field editor changes the app-managed library metadata schema,
not arbitrary PostgreSQL tables. It supports text, number, date, and yes/no
fields. Deleting one requires confirmation and deletes its associated values.
See [DOCUMENTATION.md](./DOCUMENTATION.md) for details and the full SOLID
mapping.

## Run locally

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download) and Docker
Compose.

The app targets .NET 8 and enables major runtime roll-forward, so Rider can
launch it using a newer installed shared runtime such as .NET 10. Building
still requires the .NET 8 SDK.

In PowerShell, from `Lab0`:

```powershell
Copy-Item .env.example .env
notepad .env
docker compose up -d --wait
dotnet run
```

Replace the placeholder password in `.env`, then run the app; it loads that
file automatically, including when launched from Rider. Open
<http://127.0.0.1:5080>. PostgreSQL data persists in a Docker volume.
`docker compose down -v` deletes the database and its data.

The web app binds only to this computer and has no authentication. Do not
expose it publicly without adding appropriate security.
