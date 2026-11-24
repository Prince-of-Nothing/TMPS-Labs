namespace Watchlist.Core.Storage;

public sealed record VaultCopyResult(int Entries, int Covers)
{
    public int Total => Entries + Covers;
}

/// <summary>
/// Copies missing vault files between locations without overwriting anything.
/// Entry ids already present in the target win, so choosing a folder that
/// already contains a library never creates duplicate ids.
/// </summary>
public static class VaultMigrator
{
    public static VaultCopyResult CopyVault(string sourceRoot, string targetRoot)
    {
        if (string.IsNullOrWhiteSpace(sourceRoot) ||
            string.IsNullOrWhiteSpace(targetRoot) ||
            !Directory.Exists(sourceRoot))
        {
            return new VaultCopyResult(0, 0);
        }

        var source = Path.GetFullPath(sourceRoot);
        var target = Path.GetFullPath(targetRoot);
        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
        {
            return new VaultCopyResult(0, 0);
        }

        var entries = CopyById(
            Path.Combine(source, "entries"),
            Path.Combine(target, "entries"),
            "*.md",
            separator: '-');
        var covers = CopyById(
            Path.Combine(source, "covers"),
            Path.Combine(target, "covers"),
            "*",
            separator: '.');

        return new VaultCopyResult(entries, covers);
    }

    private static int CopyById(string source, string target, string pattern, char separator)
    {
        if (!Directory.Exists(source))
        {
            return 0;
        }

        Directory.CreateDirectory(target);
        var targetIds = Directory.EnumerateFiles(target, pattern, SearchOption.TopDirectoryOnly)
            .Select(file => IdFromFileName(file, separator))
            .Where(id => id is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var copied = 0;

        foreach (var file in Directory.EnumerateFiles(source, pattern, SearchOption.TopDirectoryOnly))
        {
            var id = IdFromFileName(file, separator);
            var destination = Path.Combine(target, Path.GetFileName(file));
            if (File.Exists(destination) || (id is not null && targetIds.Contains(id)))
            {
                continue;
            }

            File.Copy(file, destination);
            copied++;
            if (id is not null)
            {
                targetIds.Add(id);
            }
        }

        return copied;
    }

    private static string? IdFromFileName(string file, char separator)
    {
        var name = Path.GetFileName(file);
        var index = name.IndexOf(separator);
        return index > 0 && int.TryParse(name[..index], out var id)
            ? id.ToString("D4")
            : null;
    }
}
