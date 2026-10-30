using Lab1.Domain.Abstractions;

namespace Lab1.Factory.Singleton;

/// <summary>
/// Singleton pattern: one shared library instance for the whole app.
/// A private constructor and a static Instance guarantee a single object.
/// </summary>
public sealed class MediaLibraryStore
{
    private static readonly Lazy<MediaLibraryStore> LazyInstance =
        new(() => new MediaLibraryStore(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Global access point to the single instance.</summary>
    public static MediaLibraryStore Instance => LazyInstance.Value;

    private MediaLibraryStore()
    {
    }

    /// <summary>The one shared library. Null until initialized.</summary>
    public IMediaLibrary? Library { get; private set; }

    public bool IsInitialized => Library is not null;

    /// <summary>
    /// Initializes the shared library once. Later calls return the same instance.
    /// </summary>
    public IMediaLibrary EnsureInitialized(IMediaLibrary library) =>
        Library ??= library;
}
