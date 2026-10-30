using Lab1.Domain.Abstractions;
using Lab1.Domain.Storage;

namespace Lab1.Factory.FactoryMethod;

/// <summary>
/// Factory Method pattern: an abstract creator that defers instantiation
/// to subclasses. Each subclass decides which IMediaLibrary to build.
/// </summary>
public abstract class MediaLibraryFactory
{
    public abstract IMediaLibrary CreateLibrary();
}

/// <summary>Creates an in-memory library.</summary>
public sealed class InMemoryLibraryFactory : MediaLibraryFactory
{
    public override IMediaLibrary CreateLibrary() => new InMemoryMediaLibrary();
}

/// <summary>Creates a JSON-backed library.</summary>
public sealed class JsonLibraryFactory : MediaLibraryFactory
{
    public override IMediaLibrary CreateLibrary() => new JsonMediaLibrary();
}
