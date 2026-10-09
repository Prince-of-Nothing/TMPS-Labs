namespace Lab0.Domain;

public sealed record MediaTag
{
    public MediaTag(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A tag name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    public string Name { get; }

    public int Id { get; init; }
}
