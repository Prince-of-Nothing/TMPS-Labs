namespace Lab0.Domain;

public sealed record MediaCategory
{
    public MediaCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A category name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    public string Name { get; }

    public int Id { get; init; }
}
