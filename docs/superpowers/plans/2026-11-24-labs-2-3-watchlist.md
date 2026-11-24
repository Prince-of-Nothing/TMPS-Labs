# Labs 2–3 Watchlist Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a working Lab 2 structural-patterns console app, a Lab 3 behavioral-patterns MAUI app, shared Markdown storage, tests and reports.

**Architecture:** Lab 2's core project holds the domain, Markdown vault and structural cover pipeline; Lab 3 references it, adds command/observer/state/strategy behavior, and drives it from a MAUI UI. One xUnit project tests both.

**Tech Stack:** C# 13, .NET 10, MAUI Android, xUnit, System.Text.Json, XDocument.

**Spec:** `docs/superpowers/specs/2026-11-24-labs-2-3-watchlist-design.md`

## Global Constraints

- Target `net10.0` for libraries/console/tests; `net10.0-android` for Lab 3.
- No server, database or OAuth. Public keyless APIs only.
- Markdown vault is the only source of truth; writes atomic; malformed files reported.
- At least 2 tests per claimed pattern.
- Reports show real class names and actual test output.

## Review Focus

- A hand-edited entry with quoted tags/unknown keys survives a save.
- A missing/broken cover never prevents an entry from being listed or saved.
- Undo after delete/import restores exactly the prior entries.
- Rating before consuming is rejected by both storage and State.
- AniList/MAL rows with missing scores/images import without crashing.

---

### Task 1: Domain + Markdown vault

**Files:** Create `Lab2/Core/Watchlist.Core.csproj`, `Domain/*`, `Storage/*`; tests in `Tests/Watchlist.Tests/MarkdownVaultTests.cs`.

**Interfaces:**
- `MediaKind`, `MediaStatus`, `MediaItem` (validated `MarkConsumed`, `Rate`, `Reopen`, `RestoreState`).
- `MediaItemBuilder.Build(int id)`.
- `IWatchlistStore.Load() : VaultLoadResult`, `Save(MediaItem)`, `Delete(int)`, `WriteIndex(items)`, `CoverFile(id, ext)`.
- `WatchlistRepository` with `Items`, `Errors`, `NextId`, `Add`, `Update`, `Delete`, `Reload`, `Find`.

- [ ] Write codec tests: round-trip, quoted tags/colon, unknown keys preserved, malformed frontmatter reported.
- [ ] Write repository tests: add/save/reload, edit, delete, index links, invalid rating.
- [ ] Implement domain + codec + atomic store + index + repository.
- [ ] `dotnet test --filter Markdown` passes.
- [ ] Commit.

### Task 2: Structural patterns + Lab 2 console

**Files:** Create `CoverSearch/*` in Core, `Lab2/Console/Lab2.csproj`, `Program.cs`, `ConsoleApp.cs`; tests in `CoverPatternTests.cs`.

**Interfaces:**
- `ICoverCatalog.SearchAsync(query, kind, ct)`.
- `CoverCandidate(Title, ThumbnailUrl, ImageUrl, Provider)`.
- `CoverCatalogDecorator`, `CachingCoverCatalog`, `FilteringCoverCatalog`.
- `ICoverImage.GetAsync(ct)`, `RemoteCoverImage`, `CachedCoverImageProxy`.
- `CoverService.SearchAsync/SaveAsync`.

- [ ] Adapter tests: Wikimedia + OpenLibrary responses map to candidates; malformed JSON yields empty/clear error.
- [ ] Decorator tests: cache hit skips inner call; filtering removes invalid/duplicate hits; stacking order.
- [ ] Proxy tests: first download writes cache, second reads cache; bad content type/size rejected; temp files cleaned.
- [ ] Implement providers, adapters, decorators, proxy, cover service.
- [ ] Implement console menu: list/add/edit/consume/rate/reopen/delete/search/pick cover/reload/help/quit.
- [ ] Run a scripted console session against a temp vault; paste output.
- [ ] Commit.

### Task 3: Behavioral core

**Files:** Create `Lab3/Behavioral/Watchlist.Behavioral.csproj`, `Commands/*`, `Observers/*`, `States/*`, `Export/*`; tests `CommandTests.cs`, `ObserverTests.cs`, `StateTests.cs`, `ExportTests.cs`.

**Interfaces:**
- `IWatchlistCommand { Description; Execute(); Undo(); }`, `CommandInvoker.Execute/Undo/Redo/CanUndo/CanRedo`.
- `WatchlistEditor` receiver wrapping repository + events.
- `WatchlistEvents.Subscribe/Unsubscribe/Publish`, `IWatchlistObserver.OnChanged(WatchlistChange)`.
- `MediaLifecycle.Consume/Rate/Reopen/Status`.
- `IWatchlistExporter { Name; Extension; Export(items) }`, CSV + Markdown.

- [ ] Command tests: add undo, delete undo, edit undo, rate undo, redo cleared on new command.
- [ ] Observer tests: all notified, unsubscribed not, stats correct, reentrancy safe.
- [ ] State tests: planned rejects rate; consume→rated; reopen clears; invalid restore rejected.
- [ ] Export tests: CSV escaping, Markdown content, runtime switch.
- [ ] Implement command/observer/state/export classes.
- [ ] `dotnet test` passes.
- [ ] Commit.

### Task 4: Import/export + MAUI Lab 3

**Files:** Create `Lab3/Behavioral/Import/*`; `Lab3/App/*` (MAUI), `Lab3/App/Lab3.csproj`; tests `ImportTests.cs`.

**Interfaces:**
- `IWatchlistImporter { Name; ImportAsync(input, ct) }`.
- `AniListImporter(username)`, `MalXmlImporter(path/xml)` returning `ImportedMedia`.
- `ImportService` dedupe by normalized title+kind and build `MediaItem`s.
- Export file writer uses `IWatchlistExporter` and shares the file.

- [ ] Tests: AniList JSON mapping (status/score/cover/missing fields), MAL XML mapping (anime+manga), duplicate detection, import command undo.
- [ ] Implement importers + import service.
- [ ] Build MAUI MainPage: cards/covers, add/edit, consume/rate/reopen, delete, undo/redo, filters, live stats toggle, reload.
- [ ] Build cover picker page: query, provider results, select, download, save cover.
- [ ] Build import page and export action.
- [ ] Build/tests pass with available toolchain; record output.
- [ ] Commit.

### Task 5: Reports, READMEs, solution, CI

**Files:** `Reports/Lab2.md`, `Reports/Lab3.md`, `Lab2/README.md`, `Lab3/README.md`, `README.md`, `TMPS-Labs.sln`, `.github/workflows/labs.yml`.

- [ ] Add projects to solution.
- [ ] Write reports with Mermaid UML, 10–30 line snippets, test names, pasted test log, conclusions.
- [ ] Add run instructions and demo transcript.
- [ ] Add CI: restore, build Lab2, test; build Lab3 APK.
- [ ] Final `dotnet test` + Lab2 demo + build evidence; commit.
