using Lab1.Domain.Abstractions;
using Lab1.Domain.Models;
using Lab1.Factory.Builder;

namespace Lab1.Domain.Storage;

/// <summary>
/// Keeps everything in memory. Uses the Builder to assemble items.
/// </summary>
public sealed class InMemoryMediaLibrary : IMediaLibrary
{
    private readonly List<MediaItem> _items = [];
    private readonly List<MediaLabel> _categories = [];
    private readonly List<MediaLabel> _tags = [];
    private int _nextCategoryId = 1;
    private int _nextTagId = 1;
    private int _nextMediaId = 1;

    public string Description => "In-memory library (nothing is saved between runs).";

    public IReadOnlyCollection<MediaItem> Items => _items;
    public IReadOnlyCollection<MediaLabel> Categories => _categories;
    public IReadOnlyCollection<MediaLabel> Tags => _tags;

    public MediaLabel AddCategory(string name)
    {
        var label = ResolveCategory(name);
        Save();
        return label;
    }

    public MediaLabel AddTag(string name)
    {
        var label = ResolveTag(name);
        Save();
        return label;
    }

    public MediaItem AddMedia(string title, string category, IEnumerable<string> tags)
    {
        var categoryLabel = ResolveCategory(category);
        var tagLabels = tags.Select(ResolveTag).ToList();

        // Builder pattern in action.
        var item = new MediaItemBuilder()
            .WithTitle(title)
            .WithCategory(categoryLabel)
            .WithTags(tagLabels)
            .Build(_nextMediaId++);

        _items.Add(item);
        Save();
        return item;
    }

    public void UpdateMedia(int id, string title, string category, IEnumerable<string> tags)
    {
        var item = FindMedia(id)
            ?? throw new InvalidOperationException($"Media item #{id} was not found.");

        item.Rename(title);
        item.Recategorize(ResolveCategory(category));
        item.SetTags(tags.Select(ResolveTag).ToList());
        Save();
    }

    public MediaItem? FindMedia(int id) => _items.FirstOrDefault(item => item.Id == id);

    public void Save()
    {
        // Nothing to persist for the in-memory library.
    }

    private MediaLabel ResolveCategory(string name) =>
        ResolveLabel(name, _categories, () => _nextCategoryId++);

    private MediaLabel ResolveTag(string name) =>
        ResolveLabel(name, _tags, () => _nextTagId++);

    private static MediaLabel ResolveLabel(string name, List<MediaLabel> labels, Func<int> nextId)
    {
        var trimmed = name.Trim();
        var existing = labels.FirstOrDefault(l =>
            string.Equals(l.Name, trimmed, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return existing;
        }

        var label = new MediaLabel(nextId(), trimmed);
        labels.Add(label);
        return label;
    }
}
