using Watchlist.Behavioral.Commands;
using Watchlist.Behavioral.Import;
using Watchlist.Core.Domain;

namespace Watchlist.Tests;

public sealed class CsvImportTests
{
    [Fact]
    public async Task Csv_import_maps_quoted_fields_and_optional_columns()
    {
        const string csv = """
        Title,Kind,Category,Tags,Status,Rating,Cover,Source
        "Dune, Part Two",Film,Sci-Fi,"space; epic",Completed,8,https://img.example/dune.jpg,https://example/dune
        Berserk,Manga,Manga,Fantasy,Reading,,https://img.example/berserk.jpg,
        """;
        var importer = new CsvWatchlistImporter();

        var rows = await importer.ImportAsync(csv);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Dune, Part Two", rows[0].Title);
        Assert.Equal(MediaKind.Film, rows[0].Kind);
        Assert.Equal(MediaStatus.Rated, rows[0].Status);
        Assert.Equal(8, rows[0].Rating);
        Assert.Equal(["space", "epic"], rows[0].Tags);
        Assert.Equal("https://img.example/dune.jpg", rows[0].CoverSourceUrl);

        Assert.Equal("Berserk", rows[1].Title);
        Assert.Equal(MediaStatus.Planned, rows[1].Status);
        Assert.Null(rows[1].Rating);
    }

    [Fact]
    public void Csv_import_rejects_a_file_without_a_title_column()
    {
        var importer = new CsvWatchlistImporter();

        var error = Assert.Throws<ImportException>(() =>
            importer.Parse("Name,Rating\nDune,8\n"));

        Assert.Contains("Title", error.Message);
    }

    [Fact]
    public void Import_progress_reports_each_added_entry()
    {
        using var fixture = new BehavioralFixture();
        var progress = new ListProgress();
        var items = new[]
        {
            fixture.NewItem("First"),
            new MediaItemBuilder().WithTitle("Second").WithKind(MediaKind.Film).Build(2),
        };

        fixture.Invoker.Execute(new ImportMediaCommand(fixture.Editor, items, progress));

        Assert.Equal(2, progress.Values.Count);
        Assert.Equal(1, progress.Values[0].Processed);
        Assert.Equal(2, progress.Values[1].Processed);
        Assert.Equal(2, progress.Values[1].Total);
        Assert.Equal("Second", progress.Values[1].Title);
    }

    private sealed class ListProgress : IProgress<ImportProgress>
    {
        public List<ImportProgress> Values { get; } = [];

        public void Report(ImportProgress value) => Values.Add(value);
    }
}
