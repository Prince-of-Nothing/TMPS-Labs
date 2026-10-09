namespace Lab0.Domain;

public sealed record WatchlistSnapshot(
    IReadOnlyList<MediaCategory> Categories,
    IReadOnlyList<MediaTag> Tags,
    IReadOnlyList<MediaEntry> Items,
    IReadOnlyList<CustomFieldDefinition> CustomFields);
