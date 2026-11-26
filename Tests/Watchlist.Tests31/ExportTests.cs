using Watchlist.Behavioral.Export;
using Watchlist.Core.Domain;

namespace Watchlist.Tests;

public sealed class ExportTests
{
    private static MediaItem Item()
    {
        var item = new MediaItemBuilder()
            .WithTitle("Dune, Part \"Two\"")
            .WithKind(MediaKind.Film)
            .WithCategory("Sci-Fi")
            .WithTags(["space", "epic"])
            .WithNotes("Line one\nLine two")
            .Build(1);
        item.MarkConsumed();
        item.Rate(9);
        return item;
    }

    [Fact]
    public void Csv_escapes_commas_quotes_and_newlines()
    {
        var csv = new CsvWatchlistExporter().Export([Item()]);

        Assert.Contains("\"Dune, Part \"\"Two\"\"\"", csv);
        Assert.Contains("\"Line one\nLine two\"", csv);
        Assert.Contains("9", csv);
    }

    [Fact]
    public void Markdown_export_contains_the_entry_and_its_notes()
    {
        var markdown = new MarkdownWatchlistExporter().Export([Item()]);

        Assert.Contains("# Watchlist export", markdown);
        Assert.Contains("### Dune, Part \"Two\"", markdown);
        Assert.Contains("**Status:** Rated (9/10)", markdown);
        Assert.Contains("Line one", markdown);
    }

    [Fact]
    public void Export_service_switches_strategy_at_runtime()
    {
        var service = new ExportService(new CsvWatchlistExporter());
        Assert.Equal("CSV", service.Name);
        Assert.StartsWith("Id,Title", service.Export([Item()]));

        service.Use(new MarkdownWatchlistExporter());

        Assert.Equal("Markdown", service.Name);
        Assert.StartsWith("# Watchlist export", service.Export([Item()]));
    }
}
