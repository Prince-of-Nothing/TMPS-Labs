# Lab 1 Report — Creational Design Patterns

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)  
**Author:** Dinu Pleșu, FAF241  
**Technology:** C# / .NET 9 MAUI (Android)  
**Year:** 2026

---

## Objectives

1. Study and understand the Creational Design Patterns.
2. Choose a domain, define its main classes/models/entities and choose the appropriate instantiation mechanisms.
3. Use some creational design patterns for object instantiation in a sample project.

---

## Domain: Consumed Media Watchlist

The domain is a **personal consumed-media watchlist**, a simplified evolution of Lab 0. A user adds media titles (films, shows, books, games), organizes them with categories and tags, marks them as consumed, and rates them from 1 to 10. The score is the rating normalized to 100.

The system is presented as a **.NET MAUI** application targeting Android. The MAUI SDK is used only for the presentation layer; the domain logic and every creational pattern are implemented by hand with plain C#.

The project is separated into modules by responsibility:

```text
Lab1/
├── MauiProgram.cs            composition root (client wiring)
├── App.xaml / AppShell.xaml  MAUI application shell
├── MainPage.xaml(.cs)        client / UI
├── Domain/
│   ├── Models/               MediaItem, MediaLabel
│   ├── Abstractions/         IMediaLibrary, IMediaScoreCalculator
│   ├── Storage/              InMemoryMediaLibrary, JsonMediaLibrary
│   ├── Scoring/              RatingScoreCalculator
│   └── Services/             WatchlistService
└── Factory/                  ← the creational patterns live here
    ├── Singleton/            MediaLibraryStore
    ├── Builder/              MediaItemBuilder
    └── FactoryMethod/        MediaLibraryFactory
```

| Module | Responsibility |
|---|---|
| **client** | `MainPage` — reads input and displays the watchlist |
| **domain** | models, storage, scoring and use-case coordination |
| **factory** | the creational patterns used to instantiate objects |
| **models** | `MediaItem`, `MediaLabel` |

<img src="../Lab1/Images/Structure.svg" width="720" alt="Project structure" />

---

## Used Patterns

Three creational design patterns are implemented:

### 1. Singleton

Singleton is a creational design pattern that ensures a class has only one instance while providing a global access point to it.

<img src="../Lab1/Images/Singleton.svg" width="600" alt="Singleton structure" />

### 2. Builder

Builder is a creational design pattern that lets you construct complex objects step by step. It allows producing different representations of an object using the same construction code.

<img src="../Lab1/Images/Builder.svg" width="600" alt="Builder structure" />

### 3. Factory Method

Factory Method provides an interface for creating objects in a superclass but lets subclasses alter the type of objects that will be created.

<img src="../Lab1/Images/FactoryMethod.svg" width="600" alt="Factory Method structure" />

---

## Implementation of Creational Design Patterns

### 1. Singleton Pattern

The **Singleton** is applied to `MediaLibraryStore` so the whole application reads and writes to the **same** media library.

**Code snippet:**

```csharp
public sealed class MediaLibraryStore
{
    private static readonly Lazy<MediaLibraryStore> LazyInstance =
        new(() => new MediaLibraryStore(), LazyThreadSafetyMode.ExecutionAndPublication);

    // Global access point to the single instance.
    public static MediaLibraryStore Instance => LazyInstance.Value;

    // Private constructor blocks direct instantiation.
    private MediaLibraryStore() { }

    public IMediaLibrary? Library { get; private set; }

    // Only the first call wins; later calls return the same instance.
    public IMediaLibrary EnsureInitialized(IMediaLibrary library) =>
        Library ??= library;
}
```

`Lazy<T>` with `ExecutionAndPublication` guarantees the instance is created in a thread-safe manner, addressing the classic multi-threading caveat of the naive Singleton.

**Usage example:**

```csharp
var library = MediaLibraryStore.Instance.EnsureInitialized(
    new JsonMediaLibrary());

// Anywhere else in the app:
var same = MediaLibraryStore.Instance.Library; // referenceEquals(library, same) == true
```

Here the first call creates and stores the library. Every subsequent call returns that identical instance, so the UI, the service and the factories all share one storage.

---

### 2. Builder Pattern

The **Builder** is applied to `MediaItemBuilder` to assemble a `MediaItem` step by step, because a watchlist entry needs a title, a category and an optional list of tags.

**Code snippet:**

```csharp
public sealed class MediaItemBuilder
{
    private string _title = string.Empty;
    private MediaLabel? _category;
    private readonly List<MediaLabel> _tags = [];

    public MediaItemBuilder WithTitle(string title) { _title = title; return this; }
    public MediaItemBuilder WithCategory(MediaLabel category) { _category = category; return this; }
    public MediaItemBuilder WithTags(IEnumerable<MediaLabel> tags) { _tags.AddRange(tags); return this; }

    public MediaItem Build(int id)
    {
        if (string.IsNullOrWhiteSpace(_title))
            throw new InvalidOperationException("A title is required.");

        var category = _category ?? throw new InvalidOperationException("A category is required.");

        var item = new MediaItem(id, _title, category);
        item.SetTags(_tags);
        return item;
    }
}
```

The fluent API keeps construction readable, and `Build()` centralises validation of the required parts.

**Usage example:**

```csharp
var item = new MediaItemBuilder()
    .WithTitle("The Hobbit")
    .WithCategory(categoryLabel)
    .WithTags(tagLabels)
    .Build(nextId);
```

The library calls this builder whenever a new entry is added.

---

### 3. Factory Method Pattern

The **Factory Method** is applied to `MediaLibraryFactory`. The client works with the abstract creator and never hard-codes which storage it receives.

**Code snippet:**

```csharp
public abstract class MediaLibraryFactory
{
    public abstract IMediaLibrary CreateLibrary();
}

public sealed class JsonLibraryFactory : MediaLibraryFactory
{
    public override IMediaLibrary CreateLibrary() => new JsonMediaLibrary();
}

public sealed class InMemoryLibraryFactory : MediaLibraryFactory
{
    public override IMediaLibrary CreateLibrary() => new InMemoryMediaLibrary();
}
```

**Usage example:**

```csharp
// Swap the persistence strategy with a one-line change.
MediaLibraryFactory libraryFactory = new JsonLibraryFactory();
var library = MediaLibraryStore.Instance.EnsureInitialized(
    libraryFactory.CreateLibrary());
```

Switching JSON storage for in-memory storage means replacing `JsonLibraryFactory` with `InMemoryLibraryFactory`; nothing else changes.

---

## Relationship to SOLID (from Lab 0)

The three patterns reinforce the SOLID principles already introduced:

- **DIP** — `WatchlistService` depends on the `IMediaLibrary` and `IMediaScoreCalculator` abstractions, not on the concrete storage or scoring classes. The factories supply the concrete instances.
- **OCP** — a new scoring policy can implement `IMediaScoreCalculator` and be selected in `MauiProgram` without editing the service.
- **SRP** — media rules, storage, coordination, scoring and UI each live in their own type.

---

## Conclusion

This laboratory work implemented three **creational design patterns** — **Singleton**, **Builder** and **Factory Method** — in a consumed-media watchlist running as a .NET MAUI (Android) application. The **Singleton** guarantees one shared library across the app, the **Builder** assembles watchlist entries step by step with readable validation, and the **Factory Method** decouples the client from the concrete storage so it can be swapped with a single line. Splitting the project into `client`, `domain` and `factory` modules keeps each responsibility separate, making the codebase easy to explain, maintain and extend in later labs.
