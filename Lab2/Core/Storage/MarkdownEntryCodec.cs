using System.Globalization;
using System.Text;
using Watchlist.Core.Domain;

namespace Watchlist.Core.Storage;

/// <summary>
/// Reads and writes one entry file. The frontmatter is a small, predictable
/// YAML subset so the vault stays readable and hand-editable.
/// </summary>
public static class MarkdownEntryCodec
{
    private static readonly string[] KnownKeys =
    [
        "id", "title", "kind", "category", "tags", "status", "rating",
        "progress", "cover", "coverSource", "source", "sourceStatus",
        "created", "updated",
    ];

    public static MediaItem Parse(string content, string sourcePath)
    {
        var text = Normalize(content);
        var lines = text.Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != "---")
        {
            throw new VaultFormatException(sourcePath, "Missing frontmatter.");
        }

        var end = Array.FindIndex(lines, 1, line => line.Trim() == "---");
        if (end < 0)
        {
            throw new VaultFormatException(sourcePath, "Frontmatter is not closed.");
        }

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < end; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                throw new VaultFormatException(sourcePath, $"Invalid frontmatter line: {line}");
            }

            var key = line[..colon].Trim();
            if (!fields.TryAdd(key, line[(colon + 1)..].Trim()))
            {
                throw new VaultFormatException(sourcePath, $"Duplicate frontmatter key: {key}");
            }
        }

        var id = RequireInt(fields, "id", sourcePath);
        var title = RequireString(fields, "title", sourcePath);
        var kind = ParseEnum(fields, "kind", MediaKind.Other, sourcePath);
        var status = ParseEnum(fields, "status", MediaStatus.Planned, sourcePath);
        var rating = OptionalInt(fields, "rating", sourcePath);
        var tags = ReadArray(fields.GetValueOrDefault("tags"), sourcePath);
        var createdAt = ReadDate(fields.GetValueOrDefault("created"), sourcePath);
        var updatedAt = ReadDate(fields.GetValueOrDefault("updated"), sourcePath);
        var cover = OptionalString(fields, "cover", sourcePath);
        if (cover is not null &&
            (cover.Contains("..", StringComparison.Ordinal) || cover.StartsWith('/') || cover.Contains(':')))
        {
            throw new VaultFormatException(sourcePath, "Cover path must stay inside the vault.");
        }

        var body = string.Join('\n', lines.Skip(end + 1)).Trim();
        if (body.StartsWith("# ", StringComparison.Ordinal))
        {
            var newline = body.IndexOf('\n');
            body = newline < 0 ? string.Empty : body[(newline + 1)..].Trim();
        }

        try
        {
            return new MediaItem(
                id,
                title,
                kind,
                OptionalString(fields, "category", sourcePath) ?? string.Empty,
                tags,
                status,
                rating,
                OptionalString(fields, "progress", sourcePath),
                cover,
                OptionalString(fields, "coverSource", sourcePath),
                OptionalString(fields, "source", sourcePath),
                OptionalString(fields, "sourceStatus", sourcePath) ?? string.Empty,
                body,
                createdAt,
                updatedAt);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new VaultFormatException(sourcePath, ex.Message);
        }
    }

    public static string Write(MediaItem item, IReadOnlyList<string>? unknownFrontmatterLines = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        Append(sb, "id", item.Id.ToString(CultureInfo.InvariantCulture));
        Append(sb, "title", Quote(item.Title));
        Append(sb, "kind", item.Kind.ToString().ToLowerInvariant());
        Append(sb, "category", Quote(item.Category));
        Append(sb, "tags", FormatArray(item.Tags));
        Append(sb, "status", item.Status.ToString().ToLowerInvariant());
        if (item.Rating is int rating)
        {
            Append(sb, "rating", rating.ToString(CultureInfo.InvariantCulture));
        }

        if (item.Progress is not null)
        {
            Append(sb, "progress", Quote(item.Progress));
        }

        if (item.CoverPath is not null)
        {
            Append(sb, "cover", Quote(item.CoverPath));
        }

        if (item.CoverSourceUrl is not null)
        {
            Append(sb, "coverSource", Quote(item.CoverSourceUrl));
        }

        if (item.SourceUrl is not null)
        {
            Append(sb, "source", Quote(item.SourceUrl));
        }

        if (!string.IsNullOrWhiteSpace(item.SourceStatus))
        {
            Append(sb, "sourceStatus", Quote(item.SourceStatus));
        }

        Append(sb, "created", item.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        Append(sb, "updated", item.UpdatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

        foreach (var line in unknownFrontmatterLines ?? [])
        {
            if (!string.IsNullOrWhiteSpace(line) &&
                !line.TrimStart().StartsWith("---", StringComparison.Ordinal))
            {
                sb.AppendLine(line.TrimEnd());
            }
        }

        sb.AppendLine("---");
        sb.Append("# ").AppendLine(item.Title);
        if (!string.IsNullOrWhiteSpace(item.Notes))
        {
            sb.AppendLine();
            sb.AppendLine(item.Notes.TrimEnd());
        }

        return sb.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    /// <summary>
    /// Keeps keys written by a human that this app does not understand.
    /// A malformed file returns nothing extra rather than guessing.
    /// </summary>
    public static IReadOnlyList<string> ExtractUnknownLines(string content)
    {
        var lines = Normalize(content).Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != "---")
        {
            return [];
        }

        var end = Array.FindIndex(lines, 1, line => line.Trim() == "---");
        if (end < 0)
        {
            return [];
        }

        var result = new List<string>();
        for (var i = 1; i < end; i++)
        {
            var line = lines[i];
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var key = line[..colon].Trim();
            if (!KnownKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(line);
            }
        }

        return result;
    }

    private static string Normalize(string? content) =>
        (content ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');

    private static void Append(StringBuilder sb, string key, string value) =>
        sb.Append(key).Append(": ").AppendLine(value);

    private static int RequireInt(Dictionary<string, string> fields, string key, string sourcePath) =>
        fields.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : throw new VaultFormatException(sourcePath, $"{key} must be a positive integer.");

    private static int? OptionalInt(Dictionary<string, string> fields, string key, string sourcePath)
    {
        if (!fields.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new VaultFormatException(sourcePath, $"{key} must be an integer.");
    }

    private static string RequireString(Dictionary<string, string> fields, string key, string sourcePath)
    {
        var value = OptionalString(fields, key, sourcePath);
        return string.IsNullOrWhiteSpace(value)
            ? throw new VaultFormatException(sourcePath, $"{key} is required.")
            : value;
    }

    private static string? OptionalString(Dictionary<string, string> fields, string key, string sourcePath) =>
        fields.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw)
            ? Unquote(raw, sourcePath)
            : null;

    private static T ParseEnum<T>(Dictionary<string, string> fields, string key, T fallback, string sourcePath)
        where T : struct, Enum
    {
        if (!fields.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        var value = Unquote(raw, sourcePath);
        return Enum.TryParse<T>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new VaultFormatException(sourcePath, $"{key} has an unknown value: {value}");
    }

    private static DateTimeOffset ReadDate(string? raw, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DateTimeOffset.UtcNow;
        }

        return DateTimeOffset.TryParse(
            Unquote(raw, sourcePath),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var value)
            ? value
            : throw new VaultFormatException(sourcePath, $"Invalid date: {raw}");
    }

    private static IReadOnlyList<string> ReadArray(string? raw, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var value = raw.Trim();
        if (!value.StartsWith('[') || !value.EndsWith(']'))
        {
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => Unquote(part, sourcePath))
                .Where(part => part.Length > 0)
                .ToArray();
        }

        return SplitArray(value[1..^1], sourcePath)
            .Select(part => Unquote(part.Trim(), sourcePath))
            .Where(part => part.Length > 0)
            .ToArray();
    }

    private static IEnumerable<string> SplitArray(string inner, string sourcePath)
    {
        var current = new StringBuilder();
        var quoted = false;
        var escaped = false;

        foreach (var ch in inner)
        {
            if (escaped)
            {
                current.Append(ch);
                escaped = false;
                continue;
            }

            if (ch == '\\' && quoted)
            {
                current.Append(ch);
                escaped = true;
                continue;
            }

            if (ch == '"')
            {
                quoted = !quoted;
                current.Append(ch);
                continue;
            }

            if (ch == ',' && !quoted)
            {
                yield return current.ToString();
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        if (quoted)
        {
            throw new VaultFormatException(sourcePath, "Unclosed quote in tags.");
        }

        yield return current.ToString();
    }

    private static string Quote(string value)
    {
        if (value.Length == 0)
        {
            return "\"\"";
        }

        var needsQuotes =
            value.Any(ch => ch is ':' or '#' or '[' or ']' or '"' or '\n' or '\r' or ',' or '{' or '}') ||
            char.IsWhiteSpace(value[0]) ||
            char.IsWhiteSpace(value[^1]) ||
            value[0] is '-' or '?' or '&' or '*' or '!' or '|' or '>' or '@' or '`';

        return needsQuotes ? $"\"{Escape(value)}\"" : value;
    }

    private static string FormatArray(IEnumerable<string> values) =>
        $"[{string.Join(", ", values.Select(Quote))}]";

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);

    private static string Unquote(string raw, string sourcePath)
    {
        var value = raw.Trim();
        if (value.Length < 2 || value[0] != '"' || value[^1] != '"')
        {
            return value;
        }

        var sb = new StringBuilder(value.Length - 2);
        var escaped = false;
        for (var i = 1; i < value.Length - 1; i++)
        {
            var ch = value[i];
            if (!escaped)
            {
                if (ch == '\\')
                {
                    escaped = true;
                }
                else
                {
                    sb.Append(ch);
                }

                continue;
            }

            sb.Append(ch switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                '\\' => '\\',
                '"' => '"',
                _ => throw new VaultFormatException(sourcePath, $"Unknown escape sequence: \\{ch}"),
            });
            escaped = false;
        }

        if (escaped)
        {
            throw new VaultFormatException(sourcePath, "Frontmatter ends with an incomplete escape.");
        }

        return sb.ToString();
    }
}
