using Watchlist.Core.Domain;
using Watchlist.Core.Storage;

namespace Watchlist.Tests;

public sealed class MarkdownVaultTests
{
    private static MediaItem Item(int id = 1, string title = "The Matrix") =>
        new MediaItemBuilder()
            .WithTitle(title)
            .WithKind(MediaKind.Film)
            .WithCategory("Sci-Fi")
            .WithTags(["action", "classic"])
            .WithNotes("Watched again.")
            .Build(id);

    [Fact]
    public void Codec_round_trip_preserves_quotes_commas_and_unicode()
    {
        var item = new MediaItemBuilder()
            .WithTitle("Dune: Part Two")
            .WithKind(MediaKind.Film)
            .WithCategory("Sci-Fi, \"Space\"")
            .WithTags(["space opera", "a,b", "quote\"x", "știință"])
            .WithNotes("Line one\nLine two: with colon")
            .Build(7);

        var text = MarkdownEntryCodec.Write(item);
        var parsed = MarkdownEntryCodec.Parse(text, "entry.md");

        Assert.Equal(item.Title, parsed.Title);
        Assert.Equal(item.Category, parsed.Category);
        Assert.Equal(item.Tags, parsed.Tags);
        Assert.Equal(item.Notes, parsed.Notes);
        Assert.Equal(item.Kind, parsed.Kind);
        Assert.Equal(item.Status, parsed.Status);
    }

    [Fact]
    public void Codec_round_trips_a_user_created_kind()
    {
        var item = new MediaItemBuilder()
            .WithTitle("Umineko")
            .WithKind(MediaKind.Custom)
            .WithCustomKind("Visual Novel")
            .Build(9);

        var parsed = MarkdownEntryCodec.Parse(
            MarkdownEntryCodec.Write(item), "custom.md");

        Assert.Equal(MediaKind.Custom, parsed.Kind);
        Assert.Equal("Visual Novel", parsed.CustomKind);
        Assert.Equal("Visual Novel", parsed.KindLabel);
    }

    [Fact]
    public void Codec_extracts_unknown_frontmatter_keys_for_later_preservation()
    {
        var text = MarkdownEntryCodec.Write(Item());
        text = text.Replace("---\n#", "mood: rewatch\nmoodNote: \"keep me\"\n---\n#", StringComparison.Ordinal);

        var unknown = MarkdownEntryCodec.ExtractUnknownLines(text);

        Assert.Contains("mood: rewatch", unknown);
        Assert.Contains("moodNote: \"keep me\"", unknown);
    }

    [Fact]
    public void Codec_rejects_rated_entry_without_rating()
    {
        var text = MarkdownEntryCodec.Write(Item())
            .Replace("status: planned", "status: rated", StringComparison.Ordinal);

        var error = Assert.Throws<VaultFormatException>(() =>
            MarkdownEntryCodec.Parse(text, "bad.md"));

        Assert.Contains("rated entry needs a rating", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Store_reports_malformed_file_without_overwriting_it()
    {
        using var temp = new TempDirectory();
        var store = new MarkdownWatchlistStore(temp.Path);
        var entries = Directory.CreateDirectory(Path.Combine(temp.Path, "entries")).FullName;
        var bad = Path.Combine(entries, "bad.md");
        File.WriteAllText(bad, "not a watchlist entry");

        var result = store.Load();

        Assert.Empty(result.Items);
        Assert.Single(result.Errors);
        Assert.Contains("Missing frontmatter", result.Errors[0].Message);
        Assert.Equal("not a watchlist entry", File.ReadAllText(bad));
    }

    [Fact]
    public void Store_preserves_unknown_frontmatter_and_manual_notes()
    {
        using var temp = new TempDirectory();
        var store = new MarkdownWatchlistStore(temp.Path);
        var item = Item();
        store.Save(item);
        var file = Directory.GetFiles(Path.Combine(temp.Path, "entries"), "*.md").Single();
        File.WriteAllText(file, File.ReadAllText(file).Replace("---\n#", "mood: rewatch\n---\n#", StringComparison.Ordinal));

        var loaded = store.Load().Items.Single();
        loaded.Edit(loaded.Title, loaded.Kind, loaded.Category, loaded.Tags, "A new note.", loaded.Progress);
        store.Save(loaded);

        var text = File.ReadAllText(file);
        Assert.Contains("mood: rewatch", text);
        Assert.Contains("A new note.", text);
        Assert.Contains("# The Matrix", text);
    }

    [Fact]
    public void Index_links_every_entry_and_groups_by_status()
    {
        using var temp = new TempDirectory();
        var store = new MarkdownWatchlistStore(temp.Path);
        var planned = Item(1, "Planned Film");
        var rated = Item(2, "Rated Film");
        rated.MarkConsumed();
        rated.Rate(9);
        store.Save(planned);
        store.Save(rated);
        store.WriteIndex([planned, rated]);

        var index = File.ReadAllText(Path.Combine(temp.Path, "index.md"));

        Assert.Contains("## Planned (1)", index);
        Assert.Contains("## Rated (1)", index);
        Assert.Contains("[Planned Film](entries/0001-planned-film.md)", index);
        Assert.Contains("9/10", index);
    }

    [Fact]
    public void Repository_add_update_delete_and_reload_round_trip()
    {
        using var temp = new TempDirectory();
        var repository = new WatchlistRepository(new MarkdownWatchlistStore(temp.Path));
        var item = Item();
        repository.Add(item);

        Assert.Single(repository.Items);
        Assert.Equal(2, repository.NextId);
        Assert.True(File.Exists(Path.Combine(temp.Path, "entries", MarkdownWatchlistStore.EntryFileName(item))));

        item.Edit("The Matrix Reloaded", MediaKind.Film, "Sci-Fi", ["sequel"], "Changed.", "2/2");
        repository.Update(item);
        repository.Reload();

        var reloaded = repository.Items.Single();
        Assert.Equal("The Matrix Reloaded", reloaded.Title);
        Assert.Equal("Changed.", reloaded.Notes);
        Assert.Equal("2/2", reloaded.Progress);
        Assert.Contains("The Matrix Reloaded", File.ReadAllText(Path.Combine(temp.Path, "index.md")));

        repository.Delete(reloaded.Id);
        repository.Reload();

        Assert.Empty(repository.Items);
        Assert.Empty(Directory.GetFiles(Path.Combine(temp.Path, "entries"), "*.md"));
    }

    [Fact]
    public void Item_rejects_rating_before_consumption()
    {
        var item = Item();

        Assert.Throws<InvalidOperationException>(() => item.Rate(8));
        item.MarkConsumed();
        item.Rate(8);

        Assert.Equal(MediaStatus.Rated, item.Status);
        Assert.Equal(8, item.Rating);
    }

    [Fact]
    public void Renamed_entry_keeps_its_original_file_name()
    {
        using var temp = new TempDirectory();
        var store = new MarkdownWatchlistStore(temp.Path);
        var item = Item();
        store.Save(item);
        var original = Directory.GetFiles(Path.Combine(temp.Path, "entries"), "*.md").Single();

        item.Edit("A different title", item.Kind, item.Category, item.Tags, item.Notes, item.Progress);
        store.Save(item);

        Assert.Equal(new[] { original }, Directory.GetFiles(Path.Combine(temp.Path, "entries"), "*.md"));
    }
}
