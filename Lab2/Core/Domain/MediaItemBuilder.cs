namespace Watchlist.Core.Domain;

/// <summary>
/// Builder from Lab 1, reused to assemble a valid entry step by step.
/// </summary>
public sealed class MediaItemBuilder
{
    private string _title = string.Empty;
    private MediaKind _kind = MediaKind.Other;
    private string _category = "Uncategorized";
    private List<string> _tags = [];
    private MediaStatus _status = MediaStatus.Planned;
    private int? _rating;
    private string? _progress;
    private string? _coverPath;
    private string? _coverSourceUrl;
    private string? _sourceUrl;
    private string _sourceStatus = string.Empty;
    private string _notes = string.Empty;
    private DateTimeOffset? _createdAt;
    private DateTimeOffset? _updatedAt;

    public MediaItemBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public MediaItemBuilder WithKind(MediaKind kind)
    {
        _kind = kind;
        return this;
    }

    public MediaItemBuilder WithCategory(string category)
    {
        _category = category;
        return this;
    }

    public MediaItemBuilder WithTags(IEnumerable<string> tags)
    {
        _tags = tags.ToList();
        return this;
    }

    public MediaItemBuilder WithStatus(MediaStatus status, int? rating = null)
    {
        _status = status;
        _rating = rating;
        return this;
    }

    public MediaItemBuilder WithProgress(string? progress)
    {
        _progress = progress;
        return this;
    }

    public MediaItemBuilder WithCover(string? relativePath, string? sourceUrl = null)
    {
        _coverPath = relativePath;
        _coverSourceUrl = sourceUrl;
        return this;
    }

    public MediaItemBuilder WithSource(string? sourceUrl, string sourceStatus = "")
    {
        _sourceUrl = sourceUrl;
        _sourceStatus = sourceStatus;
        return this;
    }

    public MediaItemBuilder WithNotes(string notes)
    {
        _notes = notes;
        return this;
    }

    public MediaItemBuilder WithTimestamps(DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        _createdAt = createdAt;
        _updatedAt = updatedAt;
        return this;
    }

    public MediaItem Build(int id) =>
        new(
            id,
            _title,
            _kind,
            _category,
            _tags,
            _status,
            _rating,
            _progress,
            _coverPath,
            _coverSourceUrl,
            _sourceUrl,
            _sourceStatus,
            _notes,
            _createdAt ?? DateTimeOffset.UtcNow,
            _updatedAt ?? _createdAt ?? DateTimeOffset.UtcNow);
}
