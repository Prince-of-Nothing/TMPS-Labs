# Lab 2 — Structural Design Patterns

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)  
**Author:** Dinu Pleșu, FAF241  
**Technology:** C# / .NET 10 MAUI (Android)  
**Instructor:** Istrati Daniel

## What it is

A local watchlist organizer built as an Android app. Entries are films, shows,
books, games, anime or manga. Each entry can have a category, tags, progress,
notes, status, rating and a cover found online and stored next to the library.

The Markdown vault remains readable without the app:

```text
library/
  index.md
  entries/0001-the-matrix.md
  covers/0001.png
```

The app uses its private app-data folder by default:
`FileSystem.AppDataDirectory/library`. Set `TMPS_LAB2_LIBRARY` to override it.

The vault codec preserves unknown frontmatter keys and notes, reports malformed
files instead of overwriting them, and writes atomically.

## Project layout

```text
Lab2/
  Core/                    Watchlist.Core (shared with Lab 3)
    Domain/                MediaItem, MediaKind, MediaStatus, MediaItemBuilder
    Storage/               MarkdownWatchlistStore, codec, index, repository
    CoverSearch/
      CoverServiceFactory  assembles the whole cover pipeline
      Providers/           Wikimedia, Open Library, TVmaze clients
      Adapters/            provider DTOs -> CoverCandidate
      Decorators/          filtering + caching
      Images/              RemoteCoverImage, CachedCoverImageProxy
  App/                     Lab 2 MAUI client
    MainPage               cards, stats, add/edit/consume/rate/cover
    EditPage               metadata form
    CoverPickerPage        provider search + local cover download
    PatternsPage           the structural chain explained in-app
    AppServices.cs         client composition root
```

`MainPage` and the other pages see only a `WatchlistRepository` and a
`CoverService`. `AppServices` asks the Core factories for those objects; the
provider clients, three adapters, routing table and decorator chain are never
constructed in the UI.

## Structural patterns

- **Adapter** — `ICoverCatalog` is the target. `WikimediaCoverAdapter`,
  `OpenLibraryCoverAdapter` and `TvMazeCoverAdapter` translate three unrelated
  public API response models into `CoverCandidate`.
- **Decorator** — `CoverCatalogDecorator` is the base;
  `CachingCoverCatalog` and `FilteringCoverCatalog` add behavior through the
  same `ICoverCatalog` interface. They can be stacked in either order.
- **Proxy** — `CachedCoverImageProxy` implements `ICoverImage` like
  `RemoteCoverImage`, serves the local file first, validates type/size and
  serializes concurrent downloads.
- Reused from Lab 1: `MediaItemBuilder` (Builder) and
  `WatchlistStoreFactory` / `MarkdownWatchlistStoreFactory` (Factory Method).

```text
MainPage -> CoverService
  -> FilteringCoverCatalog   Decorator
  -> CachingCoverCatalog     Decorator
  -> RoutingCoverCatalog
  -> provider CoverAdapter   Adapter
  -> public provider API
  -> CachedCoverImageProxy   Proxy
  -> covers/<id>.png
```

The **Patterns** button opens the same chain in the app.

## Build and run

Requirements: .NET 10 SDK, MAUI Android workload, JDK 21 and the Android SDK.

```powershell
dotnet workload install maui-android
$env:JAVA_HOME = "C:\path\to\jdk-21"
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
dotnet build .\Lab2\App\Lab2.csproj -f net10.0-android
```

APK output:
`Lab2/App/bin/Debug/net10.0-android/com.tmps.lab2.watchlist-Signed.apk`.

## Using it

- **Add** creates an entry; blank category becomes `Uncategorized`.
- **Consume** moves a planned entry to consumed.
- **Rate** (1–10) is available after consuming.
- **Reopen** returns an entry to planned.
- **Cover** searches the provider selected by the entry kind, then downloads
  the chosen image through the cache proxy.
- **Reload vault** re-reads the Markdown folder.
- **Patterns** shows the Adapter → Decorator → Proxy route.

## Tests

```powershell
dotnet test .\Tests\Watchlist.Tests\Watchlist.Tests.csproj
```

The suite covers the Markdown codec/index/repository/migration, all provider
adapters, decorator order and caching, proxy caching/validation/concurrency,
the assembled factory and the Lab 3 behavioral patterns.

Full report: [`../Reports/Lab2.md`](../Reports/Lab2.md).

A `labs-v*` tag publishes the signed Lab 2 APK, the signed Lab 3 APK and both
reports to the GitHub Release. The branch CI workflow
[`android-labs.yml`](../.github/workflows/android-labs.yml) builds both APKs.
