namespace Lab0.Infrastructure;

public static class LocalEnvironmentFile
{
    public static IReadOnlyDictionary<string, string> Read()
    {
        var filePath = FindFile();
        if (filePath is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(filePath))
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Length == 0 || trimmedLine.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmedLine.IndexOf('=');
            if (separatorIndex < 1)
            {
                continue;
            }

            var key = trimmedLine[..separatorIndex].Trim();
            var value = trimmedLine[(separatorIndex + 1)..].Trim();
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            if (key.Length > 0)
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static string? FindFile()
    {
        var directories = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        foreach (var startingDirectory in directories)
        {
            var directory = new DirectoryInfo(startingDirectory);
            while (directory is not null)
            {
                var path = Path.Combine(directory.FullName, ".env");
                if (File.Exists(path))
                {
                    return path;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }
}
