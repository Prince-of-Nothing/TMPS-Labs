namespace Lab0.Domain;

public sealed record CreateNamedItemRequest(string? Name);

public sealed record RatingRequest(int Rating);

public sealed record CustomFieldRequest(string? Name, string? Type);
