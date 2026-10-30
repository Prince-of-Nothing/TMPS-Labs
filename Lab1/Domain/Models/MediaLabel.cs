namespace Lab1.Domain.Models;

/// <summary>
/// A category or tag label. SRP: only holds identity and name.
/// </summary>
public sealed class MediaLabel(int id, string name)
{
    public int Id { get; } = id;
    public string Name { get; } = name;

    public override string ToString() => Name;
}
