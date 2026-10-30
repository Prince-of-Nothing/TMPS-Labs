using Lab1.Domain.Abstractions;
using Lab1.Domain.Models;

namespace Lab1.Domain.Services;

/// <summary>
/// Coordinates watchlist operations (SRP). Depends only on abstractions (DIP).
/// </summary>
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

    public void UpdateMedia(int id, string title, string category, string tags)
    {
        library.UpdateMedia(
            id,
            RequireName(title, "Title"),
            RequireName(category, "Category"),
            ParseTags(tags));
    }

    public void MarkAsConsumed(int id)
    {
        FindMedia(id).MarkAsConsumed();
        library.Save();
    }

    public void Rate(int id, int rating)
    {
        FindMedia(id).Rate(rating);
        library.Save();
    }

    public int? GetScore(MediaItem item) => scoreCalculator.Calculate(item.Rating);

    public IReadOnlyCollection<MediaItem> Items => library.Items;
    public IReadOnlyCollection<MediaLabel> Categories => library.Categories;
    public IReadOnlyCollection<MediaLabel> Tags => library.Tags;

    private MediaItem FindMedia(int id) =>
        library.FindMedia(id) ?? throw new InvalidOperationException($"Media item #{id} was not found.");

    private static string RequireName(string value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{field} is required.")
            : value.Trim();

    private static IEnumerable<string> ParseTags(string tags) =>
        (tags ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
