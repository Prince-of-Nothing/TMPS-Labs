using Lab0.Abstractions;
using Lab0.Domain;

namespace Lab0.Services;

public sealed class WatchlistService(
    IWatchlistRepository repository,
    IMediaScoreCalculator scoreCalculator) : IWatchlistService
{
    public async Task<WatchlistSnapshot> GetWatchlistAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.GetWatchlistAsync(cancellationToken);
        var items = snapshot.Items
            .Select(item => item with { Score = scoreCalculator.CalculateScore(item.Rating) })
            .ToArray();

        return snapshot with { Items = items };
    }

    public Task CreateCategoryAsync(string? name, CancellationToken cancellationToken = default) =>
        repository.CreateCategoryAsync(ValidateName(name, "Category"), cancellationToken);

    public Task CreateTagAsync(string? name, CancellationToken cancellationToken = default) =>
        repository.CreateTagAsync(ValidateName(name, "Tag"), cancellationToken);

    public Task<MediaEntry> AddMediaItemAsync(
        MediaItemRequest request,
        CancellationToken cancellationToken = default) =>
        repository.AddMediaItemAsync(ValidateMediaRequest(request), cancellationToken);

    public Task<MediaEntry> UpdateMediaItemAsync(
        int id,
        MediaItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id, "Media item");
        return repository.UpdateMediaItemAsync(id, ValidateMediaRequest(request), cancellationToken);
    }

    public Task MarkAsConsumedAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidateId(id, "Media item");
        return repository.MarkAsConsumedAsync(id, cancellationToken);
    }

    public Task DeleteMediaItemAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidateId(id, "Media item");
        return repository.DeleteMediaItemAsync(id, cancellationToken);
    }

    public Task RateMediaItemAsync(int id, int rating, CancellationToken cancellationToken = default)
    {
        ValidateId(id, "Media item");
        if (rating is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "A rating must be between 1 and 10.");
        }

        return repository.RateMediaItemAsync(id, rating, cancellationToken);
    }

    public Task<CustomFieldDefinition> CreateCustomFieldAsync(
        CustomFieldRequest request,
        CancellationToken cancellationToken = default) =>
        repository.SaveCustomFieldAsync(null, ValidateCustomField(request), cancellationToken);

    public Task<CustomFieldDefinition> UpdateCustomFieldAsync(
        int id,
        CustomFieldRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id, "Custom field");
        return repository.SaveCustomFieldAsync(id, ValidateCustomField(request), cancellationToken);
    }

    public Task DeleteCustomFieldAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidateId(id, "Custom field");
        return repository.DeleteCustomFieldAsync(id, cancellationToken);
    }

    private static string ValidateName(string? name, string kind)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"{kind} name is required.", nameof(name));
        }

        var normalized = name.Trim();
        if (normalized.Length > 80)
        {
            throw new ArgumentException($"{kind} name must be 80 characters or fewer.", nameof(name));
        }

        return normalized;
    }

    private static MediaItemRequest ValidateMediaRequest(MediaItemRequest? request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateId(request.CategoryId, "Category");

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200)
        {
            throw new ArgumentException("Title is required and must be 200 characters or fewer.", nameof(request));
        }

        return request with
        {
            Title = request.Title.Trim(),
            TagIds = request.TagIds?.Distinct().ToArray() ?? [],
            CustomValues = request.CustomValues ?? new Dictionary<int, System.Text.Json.JsonElement>()
        };
    }

    private static CustomFieldRequest ValidateCustomField(CustomFieldRequest? request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var name = ValidateName(request.Name, "Custom field");
        var type = request.Type?.Trim().ToLowerInvariant();
        if (type is not ("text" or "number" or "date" or "boolean"))
        {
            throw new ArgumentException("Choose a supported custom-field type.", nameof(request));
        }

        return request with { Name = name, Type = type };
    }

    private static void ValidateId(int id, string kind)
    {
        if (id < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(id), $"{kind} ID must be positive.");
        }
    }
}
