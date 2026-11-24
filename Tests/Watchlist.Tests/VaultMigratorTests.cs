using Watchlist.Core.Domain;
using Watchlist.Core.Storage;

namespace Watchlist.Tests;

public sealed class VaultMigratorTests
{
    [Fact]
    public void CopyVault_copies_entries_and_covers_without_index()
    {
        using var source = new TempDirectory();
        using var target = new TempDirectory();
        var sourceStore = new MarkdownWatchlistStore(source.Path);
        var item = new MediaItemBuilder()
            .WithTitle("The Matrix")
            .WithKind(MediaKind.Film)
            .Build(1);
        sourceStore.Save(item);
        File.WriteAllBytes(Path.Combine(sourceStore.CoversPath, "0001.jpg"), [0xFF, 0xD8, 0xFF]);
        sourceStore.WriteIndex([item]);

        var result = VaultMigrator.CopyVault(source.Path, target.Path);

        Assert.Equal(1, result.Entries);
        Assert.Equal(1, result.Covers);
        Assert.True(File.Exists(Path.Combine(target.Path, "entries", "0001-the-matrix.md")));
        Assert.True(File.Exists(Path.Combine(target.Path, "covers", "0001.jpg")));
        Assert.False(File.Exists(Path.Combine(target.Path, "index.md")));
    }

    [Fact]
    public void CopyVault_never_overwrites_an_existing_target_file()
    {
        using var source = new TempDirectory();
        using var target = new TempDirectory();
        var sourceStore = new MarkdownWatchlistStore(source.Path);
        sourceStore.Save(new MediaItemBuilder()
            .WithTitle("Source title")
            .WithKind(MediaKind.Film)
            .Build(1));
        var targetStore = new MarkdownWatchlistStore(target.Path);
        targetStore.Save(new MediaItemBuilder()
            .WithTitle("Target title")
            .WithKind(MediaKind.Film)
            .Build(1));

        var result = VaultMigrator.CopyVault(source.Path, target.Path);

        Assert.Equal(0, result.Entries);
        Assert.Contains("Target title", File.ReadAllText(
            Path.Combine(target.Path, "entries", "0001-target-title.md")));
        Assert.Single(Directory.GetFiles(Path.Combine(target.Path, "entries"), "*.md"));
    }
}
