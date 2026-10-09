using System.Text.Json;

namespace Lab0.Domain;

public sealed record MediaEntry(
    int Id,
    string Title,
    int CategoryId,
    string Category,
    MediaStatus Status,
    int? Rating,
    decimal? Score,
    IReadOnlyList<int> TagIds,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<int, JsonElement> CustomValues);
