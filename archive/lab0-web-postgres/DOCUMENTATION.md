# Lab 0 — Consumed Media Library

## Concept

Afterglow is a private, local library for films, series, books, games, and
other media. Keep a list of things to experience, organize them with
categories and tags, mark them as experienced, and score what you have
consumed. The browser interface also lets you define custom metadata fields,
similar to custom columns in a personal media catalog.

The project is a C# ASP.NET Core web app backed by PostgreSQL. It runs on
loopback (`127.0.0.1`) and does not include accounts or remote hosting.

## Goal

Demonstrate three SOLID principles in a small application:

- **SRP (Single Responsibility)** — HTTP routing, use-case coordination,
  PostgreSQL persistence, score calculation, and browser rendering are
  separated.
- **OCP (Open/Closed)** — score policies are represented by
  `IMediaScoreCalculator`; another score policy can be substituted without
  changing `WatchlistService`.
- **DIP (Dependency Inversion)** — `WatchlistService` depends on
  `IWatchlistRepository` and `IMediaScoreCalculator` abstractions. The HTTP
  endpoints depend on `IWatchlistService`, and `Program.cs` wires the
  PostgreSQL and score-calculator implementations.

## Features

- Create categories and reusable tags.
- Add, edit, search, and remove media library entries.
- Mark an entry as consumed and rate it from 1 to 10.
- Show the rating and its normalized score out of 100.
- Add, rename, and delete custom metadata fields in the browser.
- Store custom values with text, number, date, and yes/no types.
- Persist library data in PostgreSQL across application restarts.
- Validate names, ratings, foreign keys, and custom-field value types.

### What “edit the structure” means

The app provides a library-level schema editor for custom fields. Field
definitions are saved in `custom_fields`; per-entry values are stored as
typed JSONB in `custom_field_values`. The UI can add, rename, and delete
fields. Deleting a field requires confirmation and cascades its values.
Field types are kept stable when values exist, to avoid silently corrupting
or reinterpreting existing data.

This is intentionally **not** an arbitrary PostgreSQL table/column editor:
the application never accepts or executes SQL from the browser. Its core
database tables remain application-managed.

## Project structure

```text
Lab0/
├── Program.cs                              ASP.NET Core composition root and API routes
├── Abstractions/
│   ├── IMediaScoreCalculator.cs            replaceable score policy
│   ├── IWatchlistRepository.cs              persistence contract
│   └── IWatchlistService.cs                 application use-case contract
├── Domain/
│   ├── ApiRequests.cs
│   ├── CustomFieldDefinition.cs
│   ├── MediaCategory.cs
│   ├── MediaEntry.cs
│   ├── MediaItemRequest.cs
│   ├── MediaStatus.cs
│   ├── MediaTag.cs
│   └── WatchlistSnapshot.cs
├── Infrastructure/
│   └── PostgresWatchlistRepository.cs        PostgreSQL schema + data access
├── Scoring/
│   └── RatingScoreCalculator.cs              rating-to-score policy
├── Services/
│   └── WatchlistService.cs                   validation and use-case coordination
├── wwwroot/
│   ├── app.js                                browser interactions
│   ├── index.html                            library interface
│   └── styles.css                            responsive presentation
├── compose.yaml                              local PostgreSQL service
└── .env.example                              local database variable template
```

## SOLID principles

### 1. Single Responsibility Principle (SRP)

> A class should have only one reason to change.

`WatchlistService` coordinates and validates application operations.
`PostgresWatchlistRepository` owns database access and schema initialization.
`RatingScoreCalculator` calculates a score from a rating. `Program.cs`
registers dependencies and defines HTTP routes; the static browser files
render and submit the UI. Database persistence, scoring, and presentation can
change independently.

### 2. Open/Closed Principle (OCP)

> Software entities should be open for extension but closed for modification.

`WatchlistService` calculates scores through `IMediaScoreCalculator`. The
current `RatingScoreCalculator` multiplies the 1–10 user rating by ten. An
alternate score policy can implement the interface and be registered in
`Program.cs` without editing the watchlist use cases.

### 3. Dependency Inversion Principle (DIP)

> High-level modules should not depend on low-level modules; both should
> depend on abstractions.

`WatchlistService` receives `IWatchlistRepository` and
`IMediaScoreCalculator`. The HTTP routes use `IWatchlistService`; neither
needs to construct or call the PostgreSQL implementation directly.
`Program.cs` is the composition root that selects the concrete repository
and scoring policy.

## Run locally

Requirements: .NET 8 SDK and Docker with Docker Compose.

The project targets .NET 8 and enables major runtime roll-forward, so it can
also start under a newer shared runtime when the host (such as Rider) does not
have .NET 8 installed. Runtime roll-forward does not install the .NET 8 SDK;
the SDK is still needed to build the project.

1. Open a PowerShell terminal in `Lab0`.
2. Create a local environment file and replace its placeholder password with
   a unique local development password:

   ```powershell
   Copy-Item .env.example .env
   notepad .env
   ```

3. Start PostgreSQL:

   ```powershell
   docker compose up -d --wait
   ```

4. Start the application:

   ```powershell
   dotnet run
   ```

5. Open <http://127.0.0.1:5080>.

The application reads the local `.env` file automatically, including when
launched by Rider, and creates its managed tables and indexes on first startup.
Alternatively, set `ConnectionStrings__Watchlist` in Rider's run configuration.
Stop PostgreSQL with `docker compose down`; the named volume retains the
library. **`docker compose down -v` deletes that database volume and all
stored data.**

The app deliberately binds to loopback and has no login. Do not expose it to
other machines or use it as a public deployment without adding authentication,
authorization, and production database-secret management.
