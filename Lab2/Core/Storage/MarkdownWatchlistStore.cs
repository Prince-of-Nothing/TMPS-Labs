using System.Globalization;
using System.Text;
using Watchlist.Core.Domain;

namespace Watchlist.Core.Storage;

/// <summary>
/// A folder of Markdown files is the database. No app is needed to read it.
/// </summary>
public sealed class MarkdownWatchlistStore : IWatchlistStore
{
    private const string EntriesFolder = "entries";
    private const string CoversFolder = "covers";
    private const string IndexFile = "index.md";

    public MarkdownWatchlistStore(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("A vault path is required.", nameof(rootPath));
        }

        RootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(EntriesPath);
        Directory.CreateDirectory(CoversPath);
    }

    public string RootPath { get; }
    public string CoversPath => Path.Combine(RootPath, CoversFolder);
    private string EntriesPath => Path.Combine(RootPath, EntriesFolder);

    public VaultLoadResult Load()
    {
        Directory.CreateDirectory(EntriesPath);
        var items = new List<MediaItem>();
        var errors = new List<VaultError>();
        var seen = new HashSet<int>();

        foreach (var file in Directory.EnumerateFiles(EntriesPath, "*.md", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var item = MarkdownEntryCodec.Parse(File.ReadAllText(file), file);
                if (!seen.Add(item.Id))
                {
                    errors.Add(new VaultError(file, $"Duplicate entry id #{item.Id}."));
                    continue;
                }

                items.Add(item);
            }
            catch (Exception ex) when (ex is VaultFormatException or IOException or UnauthorizedAccessException)
            {
                errors.Add(new VaultError(file, ex.Message));
            }
        }

        return new VaultLoadResult(items.OrderBy(item => item.Id).ToArray(), errors);
    }

    public void Save(MediaItem item)
    {
        Directory.CreateDirectory(EntriesPath);
        var file = FindEntryFile(item.Id) ?? Path.Combine(EntriesPath, EntryFileName(item));
        var unknown = File.Exists(file)
            ? MarkdownEntryCodec.ExtractUnknownLines(File.ReadAllText(file))
            : [];
        AtomicWrite(file, MarkdownEntryCodec.Write(item, unknown));
    }

    public void Delete(int id)
    {
        var file = FindEntryFile(id);
        if (file is not null)
        {
            File.Delete(file);
        }

        if (Directory.Exists(CoversPath))
        {
            foreach (var cover in Directory.EnumerateFiles(CoversPath, $"{id:D4}.*"))
            {
                File.Delete(cover);
            }
        }
    }

    public void WriteIndex(IReadOnlyCollection<MediaItem> items)
    {
        var files = items.ToDictionary(
            item => item.Id,
            item => Path.GetFileName(FindEntryFile(item.Id) ?? EntryFileName(item)));
        AtomicWrite(Path.Combine(RootPath, IndexFile), VaultIndexWriter.Write(items, files));
    }

    public string CoverFile(int id, string extension)
    {
        var normalized = extension.StartsWith('.') ? extension : $".{extension}";
        if (normalized.Length < 2 || normalized.Length > 6 ||
            normalized[1..].Any(ch => !char.IsLetterOrDigit(ch)))
        {
            normalized = ".jpg";
        }

        return Path.Combine(CoversPath, $"{id:D4}{normalized.ToLowerInvariant()}");
    }

    public static string EntryFileName(MediaItem item) =>
        $"{item.Id:D4}-{Slug(item.Title)}.md";

    private string? FindEntryFile(int id) =>
        Directory.Exists(EntriesPath)
            ? Directory.EnumerateFiles(EntriesPath, $"{id:D4}-*.md", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault()
            : null;

    private static string Slug(string title)
    {
        var sb = new StringBuilder(title.Length);
        var lastDash = false;
        foreach (var ch in title.Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastDash = false;
            }
            else if (!lastDash && sb.Length > 0)
            {
                sb.Append('-');
                lastDash = true;
            }
        }

        var slug = sb.ToString().Trim('-');
        if (slug.Length > 60)
        {
            slug = slug[..60].Trim('-');
        }

        return slug.Length == 0 ? "entry" : slug;
    }

    private static void AtomicWrite(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temp, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }
}
