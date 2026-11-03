using Lab1.Domain.Models;

namespace Lab1.Factory.Builder;

/// <summary>
/// Builder pattern: assembles a MediaItem step by step with a fluent API.
/// </summary>
public sealed class MediaItemBuilder
{
    private string _title = string.Empty;
    private MediaLabel? _category;
    private readonly List<MediaLabel> _tags = [];

    public MediaItemBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public MediaItemBuilder WithCategory(MediaLabel category)
    {
        _category = category;
        return this;
    }

    public MediaItemBuilder AddTag(MediaLabel tag)
    {
        _tags.Add(tag);
        return this;
    }

    public MediaItemBuilder WithTags(IEnumerable<MediaLabel> tags)
    {
        _tags.AddRange(tags);
        return this;
    }

    public MediaItem Build(int id)
    {
        if (string.IsNullOrWhiteSpace(_title))
        {
            throw new InvalidOperationException("A title is required.");
        }

        var category = _category ?? throw new InvalidOperationException("A category is required.");

        var item = new MediaItem(id, _title, category);
        item.SetTags(_tags);
        return item;
    }
}
