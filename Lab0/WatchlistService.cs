namespace Lab0;

public sealed class WatchlistService(IMediaLibrary library, IMediaScoreCalculator scoreCalculator)
{
    public MediaLabel AddCategory(string name) => library.AddCategory(RequireName(name, "Category"));

    public MediaLabel AddTag(string name) => library.AddTag(RequireName(name, "Tag"));

    public void AddMedia(string title, string category, string tags)
    {
        library.AddMedia(
            RequireName(title, "Title"),
            RequireName(category, "Category"),
            ParseTags(tags));
    }

    public void UpdateMedia(int id, string title, string category, string tags) =>
        library.UpdateMedia(
            id,
            RequireName(title, "Title"),
            RequireName(category, "Category"),
            ParseTags(tags));

    public void MarkAsConsumed(int id) => FindMedia(id).MarkAsConsumed();

    public void Rate(int id, int rating) => FindMedia(id).Rate(rating);

    public int? GetScore(MediaItem item) => scoreCalculator.Calculate(item.Rating);

    public IReadOnlyCollection<MediaItem> Items => library.Items;

    public IReadOnlyCollection<MediaLabel> Categories => library.Categories;

    public IReadOnlyCollection<MediaLabel> Tags => library.Tags;

    private MediaItem FindMedia(int id) =>
        library.FindMedia(id) ?? throw new KeyNotFoundException($"No media item has ID {id}.");

    private static string RequireName(string value, string label) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{label} cannot be empty.")
            : value.Trim();

    private static string[] ParseTags(string tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
