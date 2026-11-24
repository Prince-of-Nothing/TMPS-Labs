# Lab 2 — Structural Design Patterns

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)  
**Author:** Dinu Pleșu, FAF241  
**Technology:** C# / .NET 10 console  
**Instructor:** Istrati Daniel

## What it is

A local watchlist organizer. Entries are films, shows, books, games, anime or
manga. Each entry can have a category, tags, progress, notes, status, rating
and a cover found online and stored next to the library.

The library is a Markdown vault; the app is optional:

```text
library/
  index.md                 generated overview
  entries/0001-the-matrix.md
  covers/0001.jpg
```

```md
---
id: 1
title: The Matrix
kind: film
category: Sci-Fi
tags: [action, classic]
status: rated
rating: 9
progress: 1/1
cover: covers/0001.jpg
coverSource: "https://..."
created: 2026-11-24T...
updated: 2026-11-24T...
---
# The Matrix

Watched on a rainy night.
```

Unknown frontmatter keys and the notes body are preserved when the app saves.
Malformed files are reported, not overwritten. Writes are atomic.

## Project layout

```text
Lab2/
  Core/                    Watchlist.Core (shared with Lab 3)
    Domain/                MediaItem, MediaKind, MediaStatus, MediaItemBuilder
    Storage/               MarkdownWatchlistStore, codec, index, repository
    CoverSearch/
      Providers/           Wikimedia, Open Library, TVmaze clients
      Adapters/            provider DTOs -> CoverCandidate
      Decorators/          filtering + caching
      Images/              RemoteCoverImage, CachedCoverImageProxy
  Console/                 Lab 2 client (composition + menu)
```

## Structural patterns

- **Adapter** — `ICoverCatalog` is the target. `WikimediaCoverAdapter`,
  `OpenLibraryCoverAdapter` and `TvMazeCoverAdapter` translate three unrelated
  third-party response models into `CoverCandidate`. The adaptees are public
  APIs the project does not own.
- **Decorator** — `CoverCatalogDecorator` is the base; `CachingCoverCatalog`
  skips repeated provider calls and `FilteringCoverCatalog` removes unusable,
  duplicate and unsupported links. Both have the same `ICoverCatalog`
  interface and can be stacked in either order.
- **Proxy** — `CachedCoverImageProxy` implements `ICoverImage` like the real
  `RemoteCoverImage`, checks the local file first, validates type and size, and
  writes atomically. Concurrent requests for one cover share a single download.
- Reused from Lab 1: `MediaItemBuilder` (Builder) and
  `WatchlistStoreFactory` / `MarkdownWatchlistStoreFactory` (Factory Method).

Cover search and download are the interacting execution path:

```text
ConsoleApp
  -> ICoverCatalog (FilteringCoverCatalog)
  -> ICoverCatalog (CachingCoverCatalog)
  -> RoutingCoverCatalog -> provider adapter -> public API
  -> CoverService -> ICoverImage (CachedCoverImageProxy)
  -> ICoverImage (RemoteCoverImage) -> covers/<id>.jpg
```

`patterns` in the console prints this chain.

## Run it

```powershell
dotnet run --project .\Lab2\Console\Lab2.csproj
```

The default vault is `%LOCALAPPDATA%\TMPS-Labs\Lab2\library`. Set
`TMPS_LAB2_LIBRARY` to use another folder (the demo transcript used a temp
folder).

Commands: `list`, `show`, `add`, `edit`, `consume`, `rate`, `reopen`, `delete`,
`search`, `cover`, `reload`, `patterns`, `help`, `quit`.

```text
watchlist> add
Title: The Matrix
...
watchlist> search
Entry id: 1
Search covers for [The Matrix]: The Matrix poster
  [1] ...
Pick a cover number: 1
Saved covers/0001.jpg (63.2 KB).
```

## Tests

```powershell
dotnet test .\Tests\Watchlist.Tests\Watchlist.Tests.csproj
```

The suite covers the Markdown codec/index/repository, all three providers'
adapters, decorator order and caching, proxy caching/validation/concurrency,
and the Lab 3 behavioral patterns.

Full report: [`../Reports/Lab2.md`](../Reports/Lab2.md).

A `labs-v*` tag also publishes a self-contained `Lab2-win-x64.zip` in the
GitHub Release, together with the Lab 3 APK and both reports.
