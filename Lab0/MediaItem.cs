namespace Lab0;

public sealed class MediaItem
{
    public MediaItem(int id, string title, MediaLabel category, IReadOnlyList<MediaLabel> tags)
    {
        Id = id;
        Title = title;
        Category = category;
        Tags = tags;
    }

    public int Id { get; }

    public string Title { get; private set; }

    public MediaLabel Category { get; private set; }

    public IReadOnlyList<MediaLabel> Tags { get; private set; }

    public bool IsConsumed { get; private set; }

    public int? Rating { get; private set; }

    public void UpdateDetails(string title, MediaLabel category, IReadOnlyList<MediaLabel> tags)
    {
        Title = title;
        Category = category;
        Tags = tags;
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
