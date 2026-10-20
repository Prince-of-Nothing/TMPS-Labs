using System.Text.Json;

namespace Lab0;

public sealed class JsonMediaLibrary : IMediaLibrary
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly InMemoryMediaLibrary _library = new();

    public JsonMediaLibrary(string? filePath = null)
    {
        var configuredPath = filePath ?? Environment.GetEnvironmentVariable("TMPS_LAB0_WATCHLIST_PATH");
        FilePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TMPS-Labs",
                "Lab0",
                "watchlist.json")
            : Path.GetFullPath(configuredPath);

        if (File.Exists(FilePath))
        {
            Load();
        }
    }

    public string FilePath { get; }

    public IReadOnlyCollection<MediaItem> Items => _library.Items;

    public IReadOnlyCollection<MediaLabel> Categories => _library.Categories;

    public IReadOnlyCollection<MediaLabel> Tags => _library.Tags;

    public MediaLabel AddCategory(string category)
    {
        var label = _library.AddCategory(category);
        Save();
        return label;
    }

    public MediaLabel AddTag(string tag)
    {
        var label = _library.AddTag(tag);
        Save();
        return label;
    }

    public MediaItem AddMedia(string title, string category, IEnumerable<string> tags)
    {
        var item = _library.AddMedia(title, category, tags);
        Save();
        return item;
    }

    public void UpdateMedia(int id, string title, string category, IEnumerable<string> tags)
    {
        _library.UpdateMedia(id, title, category, tags);
        Save();
    }

    public MediaItem? FindMedia(int id) => _library.FindMedia(id);

    public void Save()
    {
        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("The watchlist file must have a parent directory.");
        Directory.CreateDirectory(directory);

        var data = new WatchlistData(
            1,
            Categories.Select(label => new LabelData(label.Id, label.Name)).ToList(),
            Tags.Select(label => new LabelData(label.Id, label.Name)).ToList(),
            Items.Select(item => new MediaData(
                item.Id,
                item.Title,
                item.Category.Id,
                item.Tags.Select(tag => tag.Id).ToList(),
                item.IsConsumed,
                item.Rating)).ToList());

        File.WriteAllText(FilePath, JsonSerializer.Serialize(data, JsonOptions));
    }

    private void Load()
    {
        var json = File.ReadAllText(FilePath);
        var data = JsonSerializer.Deserialize<WatchlistData>(json)
            ?? throw new InvalidDataException("The watchlist JSON file is empty or invalid.");
        if (data.Version != 1)
        {
            throw new InvalidDataException($"Unsupported watchlist JSON version: {data.Version}.");
        }

        if (data.Categories is null || data.Tags is null || data.Items is null)
        {
            throw new InvalidDataException("The watchlist JSON file is missing required collections.");
        }

        foreach (var label in data.Categories.OrderBy(label => label.Id))
        {
            ValidateId(_library.AddCategory(label.Name).Id, label.Id, "category");
        }

        foreach (var label in data.Tags.OrderBy(label => label.Id))
        {
            ValidateId(_library.AddTag(label.Name).Id, label.Id, "tag");
        }

        foreach (var savedItem in data.Items.OrderBy(item => item.Id))
        {
            if (savedItem.TagIds is null)
            {
                throw new InvalidDataException(
                    $"The watchlist JSON file is missing tag IDs for media item {savedItem.Id}.");
            }

            var item = _library.AddMedia(
                savedItem.Title,
                $"#{savedItem.CategoryId}",
                savedItem.TagIds.Select(id => $"#{id}"));
            ValidateId(item.Id, savedItem.Id, "media item");

            if (savedItem.IsConsumed)
            {
                item.MarkAsConsumed();
            }

            if (savedItem.Rating is int rating)
            {
                item.Rate(rating);
            }
        }
    }

    private static void ValidateId(int actualId, int expectedId, string entity)
    {
        if (actualId != expectedId)
        {
            throw new InvalidDataException(
                $"The watchlist JSON file contains a non-sequential {entity} ID: {expectedId}.");
        }
    }

    private sealed record WatchlistData(
        int Version,
        List<LabelData>? Categories,
        List<LabelData>? Tags,
        List<MediaData>? Items);

    private sealed record LabelData(int Id, string Name);

    private sealed record MediaData(
        int Id,
        string Title,
        int CategoryId,
        List<int>? TagIds,
        bool IsConsumed,
        int? Rating);
}
