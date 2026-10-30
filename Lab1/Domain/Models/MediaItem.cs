namespace Lab1.Domain.Models;

/// <summary>
/// A single watchlist entry. SRP: owns its state and rating rules only.
/// </summary>
public sealed class MediaItem
{
    private readonly List<MediaLabel> _tags = [];

    public MediaItem(int id, string title, MediaLabel category)
    {
        Id = id;
        Title = title;
        Category = category;
    }

    public int Id { get; }
    public string Title { get; private set; }
    public MediaLabel Category { get; private set; }
    public IReadOnlyCollection<MediaLabel> Tags => _tags;
    public bool IsConsumed { get; private set; }
    public int? Rating { get; private set; }

    public void Rename(string title) => Title = title;

    public void Recategorize(MediaLabel category) => Category = category;

    public void SetTags(IEnumerable<MediaLabel> tags)
    {
        _tags.Clear();
        _tags.AddRange(tags);
    }

    public void MarkAsConsumed() => IsConsumed = true;

    public void Rate(int rating)
    {
        if (!IsConsumed)
        {
            throw new InvalidOperationException("Mark the media as consumed before rating it.");
        }

        if (rating is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be from 1 to 10.");
        }

        Rating = rating;
    }
}
