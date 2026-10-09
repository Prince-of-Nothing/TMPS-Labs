namespace Lab0;

public interface IMediaLibrary
{
    IReadOnlyCollection<MediaItem> Items { get; }

    IReadOnlyCollection<MediaLabel> Categories { get; }

    IReadOnlyCollection<MediaLabel> Tags { get; }

    MediaLabel AddCategory(string category);

    MediaLabel AddTag(string tag);

    MediaItem AddMedia(string title, string category, IEnumerable<string> tags);

    void UpdateMedia(int id, string title, string category, IEnumerable<string> tags);

    MediaItem? FindMedia(int id);
}
