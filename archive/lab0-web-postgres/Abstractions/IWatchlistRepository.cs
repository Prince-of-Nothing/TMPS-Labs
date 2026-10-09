using Lab0.Domain;

namespace Lab0.Abstractions;

public interface IWatchlistRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<WatchlistSnapshot> GetWatchlistAsync(CancellationToken cancellationToken = default);

    Task CreateCategoryAsync(string name, CancellationToken cancellationToken = default);

    Task CreateTagAsync(string name, CancellationToken cancellationToken = default);

    Task<MediaEntry> AddMediaItemAsync(MediaItemRequest request, CancellationToken cancellationToken = default);

    Task<MediaEntry> UpdateMediaItemAsync(int id, MediaItemRequest request, CancellationToken cancellationToken = default);

    Task DeleteMediaItemAsync(int id, CancellationToken cancellationToken = default);

    Task MarkAsConsumedAsync(int id, CancellationToken cancellationToken = default);

    Task RateMediaItemAsync(int id, int rating, CancellationToken cancellationToken = default);

    Task<CustomFieldDefinition> SaveCustomFieldAsync(
        int? id,
        CustomFieldRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteCustomFieldAsync(int id, CancellationToken cancellationToken = default);
}
