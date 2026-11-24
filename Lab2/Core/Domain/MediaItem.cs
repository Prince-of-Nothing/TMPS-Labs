namespace Watchlist.Core.Domain;

/// <summary>
/// A single watchlist entry. It owns only its data and the rules that keep
/// status, rating and cover values consistent.
/// </summary>
public sealed class MediaItem
{
    private readonly List<string> _tags;

    public MediaItem(
        int id,
        string title,
        MediaKind kind,
        string category,
        IEnumerable<string> tags,
        MediaStatus status,
        int? rating,
        string? progress,
        string? coverPath,
        string? coverSourceUrl,
        string? sourceUrl,
        string sourceStatus,
        string notes,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Entry id must be positive.");
        }

        Id = id;
        Title = Require(title, "Title");
        Kind = kind;
        Category = string.IsNullOrWhiteSpace(category) ? "Uncategorized" : category.Trim();
        _tags = NormalizeTags(tags);
        Notes = notes ?? string.Empty;
        Progress = NullIfWhiteSpace(progress);
        CoverPath = NormalizeRelativePath(coverPath);
        CoverSourceUrl = NullIfWhiteSpace(coverSourceUrl);
        SourceUrl = NullIfWhiteSpace(sourceUrl);
        SourceStatus = sourceStatus?.Trim() ?? string.Empty;
        CreatedAt = createdAt == default ? DateTimeOffset.UtcNow : createdAt;
        UpdatedAt = updatedAt == default ? CreatedAt : updatedAt;

        RestoreState(status, rating);
    }

    public int Id { get; }
    public string Title { get; private set; }
    public MediaKind Kind { get; private set; }
    public string Category { get; private set; }
    public IReadOnlyList<string> Tags => _tags;
    public MediaStatus Status { get; private set; }
    public int? Rating { get; private set; }
    public string? Progress { get; private set; }
    public string? CoverPath { get; private set; }
    public string? CoverSourceUrl { get; private set; }
    public string? SourceUrl { get; private set; }
    public string SourceStatus { get; private set; }
    public string Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Edit(
        string title,
        MediaKind kind,
        string category,
        IEnumerable<string> tags,
        string notes,
        string? progress)
    {
        Title = Require(title, "Title");
        Kind = kind;
        Category = string.IsNullOrWhiteSpace(category) ? "Uncategorized" : category.Trim();
        _tags.Clear();
        _tags.AddRange(NormalizeTags(tags));
        Notes = notes ?? string.Empty;
        Progress = NullIfWhiteSpace(progress);
    }

    public void MarkConsumed()
    {
        if (Status == MediaStatus.Planned)
        {
            Status = MediaStatus.Consumed;
        }
    }

    public void Rate(int rating)
    {
        if (Status == MediaStatus.Planned)
        {
            throw new InvalidOperationException("Mark the media as consumed before rating it.");
        }

        if (rating is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be from 1 to 10.");
        }

        Rating = rating;
        Status = MediaStatus.Rated;
    }

    public void Reopen()
    {
        Status = MediaStatus.Planned;
        Rating = null;
    }

    public void RestoreState(MediaStatus status, int? rating)
    {
        if (status == MediaStatus.Rated && rating is null)
        {
            throw new ArgumentException("A rated entry needs a rating from 1 to 10.", nameof(rating));
        }

        if (status != MediaStatus.Rated && rating is not null)
        {
            throw new ArgumentException("Only a rated entry can have a rating.", nameof(rating));
        }

        if (rating is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be from 1 to 10.");
        }

        Status = status;
        Rating = rating;
    }

    public void SetCover(string? relativePath, string? sourceUrl)
    {
        CoverPath = NormalizeRelativePath(relativePath);
        CoverSourceUrl = NullIfWhiteSpace(sourceUrl);
    }

    public void SetSource(string? sourceUrl, string sourceStatus)
    {
        SourceUrl = NullIfWhiteSpace(sourceUrl);
        SourceStatus = sourceStatus?.Trim() ?? string.Empty;
    }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private static string Require(string value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{field} is required.", field)
            : value.Trim();

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<string> NormalizeTags(IEnumerable<string>? tags) =>
        (tags ?? [])
            .Select(tag => tag?.Trim() ?? string.Empty)
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string? NormalizeRelativePath(string? path) =>
        NullIfWhiteSpace(path)?.Replace('\\', '/').TrimStart('/');
}
