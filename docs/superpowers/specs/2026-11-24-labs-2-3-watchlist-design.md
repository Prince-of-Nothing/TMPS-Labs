# Labs 2–3 Watchlist — Design

**Date:** 2026-11-24  
**Course:** TMPS  
**Author:** Dinu Pleșu, FAF241

## Intent

Deliver Lab 2 (structural patterns) and Lab 3 (behavioral patterns) as one
watchlist product with two clearly different clients. Both read and write a
human-readable Markdown vault so the library survives without the app.

## Goals

- Lab 2: a runnable console organizer that implements Adapter, Decorator and
  Proxy with full structure; patterns interact through the cover workflow.
- Lab 3: a MAUI Android tracker built on Lab 2's core, implementing Command,
  Observer, State and Strategy with runtime behavior changes.
- Reuse Lab 1 creational ideas: MediaItemBuilder and a Factory Method for the
  store.
- Local-first Markdown library, local covers, AniList and MAL XML import,
  CSV/Markdown export.
- Unit tests: at least two per claimed pattern, runnable with one command.
- Reports with real class names, Mermaid UML, code snippets, test evidence and
  conclusions.

## Non-goals

- No server, account system or database.
- No OAuth login to MAL/AniList; AniList reads public lists, MAL reads its
  exported XML.
- No cover editing, image generation or media playback.

## Decisions

- **A**: Lab 2 is a console client; Lab 3 is an Android MAUI client; shared
  domain/structural code lives in Lab 2's core project so Lab 3 literally
  references and extends it.
- **Markdown vault**: `index.md` + `entries/*.md` + `covers/`. One entry per
  file with YAML-style frontmatter and a free-form notes body. Unknown
  frontmatter keys are preserved; malformed files are reported and never
  overwritten. Writes are atomic.
- **Cover search** uses public keyless providers: Wikimedia Commons (general),
  Open Library (books), TVmaze (shows). Providers sit behind one catalog
  interface, so a new provider is a new class only.
- **Import** accepts an AniList public username (GraphQL) or a MAL export XML
  file. Duplicates are matched by normalized title + kind.
- **Export** is a Strategy: CSV or Markdown, chosen at runtime.

## Architecture

```text
Lab2/Core (Watchlist.Core)
  Domain/                  MediaItem, MediaKind, MediaStatus, MediaItemBuilder
  Storage/                 MarkdownWatchlistStore, codec, index, factory
  CoverSearch/             ICoverCatalog, adapters, decorators, image proxy
Lab2/Console (Lab2)        menu client; composition root for Lab 2
Lab3/Behavioral            commands, observers, lifecycle states, importers,
                           export strategies
Lab3/App (Lab3)            Android MAUI client; composition root for Lab 3
Tests/Watchlist.Tests      xUnit tests for both labs
```

Lab 2 execution path:

```text
Console -> ICoverCatalog (Filtering -> Caching -> provider Adapter)
        -> CoverService -> ICoverImage (CachedCoverImageProxy -> RemoteCoverImage)
        -> covers/<id>.jpg + entry frontmatter
```

Lab 3 execution path:

```text
UI -> IWatchlistCommand -> WatchlistEditor -> MarkdownWatchlistStore
       -> WatchlistEvents -> StatisticsObserver / ActivityLogObserver / UI
       -> MediaLifecycle (Planned -> Consumed -> Rated) on lifecycle commands
       -> IWatchlistExporter (CSV or Markdown, switched at runtime)
```

## Pattern assignments

| Lab | Pattern | Roles |
|---|---|---|
| 2 | Adapter | `ICoverCatalog` target; `WikimediaCoverAdapter`, `OpenLibraryCoverAdapter`, `TvMazeCoverAdapter`; third-party clients/APIs are adaptees |
| 2 | Decorator | `CoverCatalogDecorator` base; `CachingCoverCatalog`, `FilteringCoverCatalog`; `ICoverCatalog` component |
| 2 | Proxy | `ICoverImage`; `RemoteCoverImage` real subject; `CachedCoverImageProxy` stand-in with disk cache and size/type checks |
| 2 | Builder (reused) | `MediaItemBuilder` |
| 2 | Factory Method (reused) | `WatchlistStoreFactory` / `MarkdownWatchlistStoreFactory` |
| 3 | Command | `IWatchlistCommand`, add/edit/delete/consume/rate/cover/import commands, `CommandInvoker` with undo/redo |
| 3 | Observer | `WatchlistEvents` subject; `IWatchlistObserver`; statistics, activity log and UI observers |
| 3 | State | `MediaLifecycle` context; `IMediaState`; Planned, Consumed, Rated states with guarded transitions |
| 3 | Strategy | `IWatchlistExporter`; CSV and Markdown exporters; runtime format switch |

## Markdown format

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
coverSource: https://...
source: https://anilist.co/anime/...
sourceStatus: COMPLETED
created: 2026-11-24T12:00:00Z
updated: 2026-11-24T12:00:00Z
---
# The Matrix

Notes...
```

`index.md` is generated from the entries and grouped by status. The entries
are the source of truth; editing a file by hand and pressing Reload is valid.

## Verification

- `dotnet test` for all pattern and codec/import/export behavior.
- `dotnet run --project Lab2/Console` with a scripted session covering add,
  search, cover cache, consume and rate.
- `dotnet build Lab3/App -f net10.0-android` when the MAUI Android workload is
  installed; CI workflow builds the APK.
- Reports in `Reports/Lab2.md` and `Reports/Lab3.md`; READMEs in each lab.

## Risks

- Public image APIs can change or rate-limit; adapters return clear errors and
  the app keeps working without a cover.
- Android build needs the MAUI workload + Android SDK; the domain and tests do
  not.
- 200-entry imports can be slow if covers are downloaded eagerly; import stores
  remote cover URLs first, and the user can choose a local cover per item.
