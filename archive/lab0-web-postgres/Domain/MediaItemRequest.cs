using System.Text.Json;

namespace Lab0.Domain;

public sealed record MediaItemRequest(
    string? Title,
    int CategoryId,
    IReadOnlyList<int>? TagIds,
    IReadOnlyDictionary<int, JsonElement>? CustomValues);
