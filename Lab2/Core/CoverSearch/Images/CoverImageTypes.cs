namespace Watchlist.Core.CoverSearch.Images;

public static class CoverImageTypes
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
    };

    public static string ExtensionForUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return ".jpg";
        }

        var extension = Path.GetExtension(uri.AbsolutePath);
        return ContentTypes.ContainsKey(extension) ? extension.ToLowerInvariant() : ".jpg";
    }

    public static string ExtensionForContentType(string? contentType)
    {
        var match = ContentTypes.FirstOrDefault(pair =>
            string.Equals(pair.Value, contentType, StringComparison.OrdinalIgnoreCase));
        return match.Key ?? ".jpg";
    }

    public static string ContentTypeForPath(string path)
    {
        var extension = Path.GetExtension(path);
        return ContentTypes.GetValueOrDefault(extension, "application/octet-stream");
    }

    public static bool LooksLikeImage(byte[] bytes)
    {
        if (bytes.Length < 4)
        {
            return false;
        }

        return (bytes[0] == 0xFF && bytes[1] == 0xD8) || // JPEG
               (bytes[0] == 0x89 && bytes[1] == 0x50) || // PNG
               (bytes[0] == 'G' && bytes[1] == 'I' && bytes[2] == 'F') ||
               (bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F') ||
               (bytes[0] == 'B' && bytes[1] == 'M');
    }
}
