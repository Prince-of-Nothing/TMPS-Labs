namespace Lab0;

public sealed class InMemoryMediaLibrary : IMediaLibrary
{
    private readonly List<MediaItem> _items = [];
    private readonly List<MediaLabel> _categories = [];
    private readonly List<MediaLabel> _tags = [];
    private int _nextId = 1;
    private int _nextCategoryId = 1;
    private int _nextTagId = 1;

    public IReadOnlyCollection<MediaItem> Items => _items.AsReadOnly();

    public IReadOnlyCollection<MediaLabel> Categories => _categories.AsReadOnly();

    public IReadOnlyCollection<MediaLabel> Tags => _tags.AsReadOnly();

    public MediaLabel AddCategory(string category) => GetOrAddLabel(_categories, category, ref _nextCategoryId);

    public MediaLabel AddTag(string tag) => GetOrAddLabel(_tags, tag, ref _nextTagId);

    public MediaItem AddMedia(string title, string category, IEnumerable<string> tags)
    {
        var item = new MediaItem(_nextId++, title.Trim(), ResolveCategory(category), ResolveTags(tags));
        _items.Add(item);
        return item;
    }

    public void UpdateMedia(int id, string title, string category, IEnumerable<string> tags)
    {
        var item = FindMedia(id) ?? throw new KeyNotFoundException($"No media item has ID {id}.");
        item.UpdateDetails(title.Trim(), ResolveCategory(category), ResolveTags(tags));
    }

    public MediaItem? FindMedia(int id) => _items.FirstOrDefault(item => item.Id == id);

    public void Save()
    {
    }

    private MediaLabel ResolveCategory(string category) =>
        ResolveLabel(category, _categories, AddCategory, "category");

    private MediaLabel[] ResolveTags(IEnumerable<string> tags) =>
        tags.Select(tag => ResolveLabel(tag, _tags, AddTag, "tag"))
            .DistinctBy(tag => tag.Id)
            .ToArray();

    private MediaLabel ResolveLabel(
        string input,
        IReadOnlyCollection<MediaLabel> labels,
        Func<string, MediaLabel> getOrAddByName,
        string kind)
    {
        var value = input.Trim();
        var idValue = value.StartsWith('#') ? value[1..] : value;
        if (int.TryParse(idValue, out var id))
        {
            return labels.FirstOrDefault(label => label.Id == id)
                ?? throw new KeyNotFoundException($"No {kind} has ID {id}.");
        }

        if (value.StartsWith('#'))
        {
            throw new ArgumentException($"Enter a valid {kind} name or ID.", nameof(input));
        }

        return labels.FirstOrDefault(label =>
            label.Name.Equals(value, StringComparison.OrdinalIgnoreCase))
            ?? getOrAddByName(value);
    }

    private static MediaLabel GetOrAddLabel(
        ICollection<MediaLabel> labels,
        string name,
        ref int nextId)
    {
        var normalized = name.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("A name is required.", nameof(name));
        }

        var existing = labels.FirstOrDefault(label =>
            label.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var label = new MediaLabel(nextId++, normalized);
        labels.Add(label);
        return label;
    }
}
