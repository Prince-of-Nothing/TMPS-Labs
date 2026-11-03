using Lab1.Domain.Models;

namespace Lab1.Domain.Abstractions;

/// <summary>
/// Storage abstraction used by the service (DIP).
/// </summary>
public interface IMediaLibrary
{
    string Description { get; }
    IReadOnlyCollection<MediaItem> Items { get; }
    IReadOnlyCollection<MediaLabel> Categories { get; }
    IReadOnlyCollection<MediaLabel> Tags { get; }

    MediaLabel AddCategory(string name);
    MediaLabel AddTag(string name);
    MediaItem AddMedia(string title, string category, IEnumerable<string> tags);
    void UpdateMedia(int id, string title, string category, IEnumerable<string> tags);
    MediaItem? FindMedia(int id);
    void Save();
}
