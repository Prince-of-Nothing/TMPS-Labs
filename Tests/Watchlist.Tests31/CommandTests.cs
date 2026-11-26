using Watchlist.Behavioral.Commands;
using Watchlist.Behavioral.States;
using Watchlist.Core.Domain;

namespace Watchlist.Tests;

public sealed class CommandTests
{
    [Fact]
    public void Add_command_undo_removes_and_redo_restores()
    {
        using var fixture = new BehavioralFixture();
        var item = fixture.NewItem();
        var command = new AddMediaCommand(fixture.Editor, item);

        fixture.Invoker.Execute(command);
        Assert.Single(fixture.Editor.Items);

        fixture.Invoker.Undo();
        Assert.Empty(fixture.Editor.Items);

        fixture.Invoker.Redo();
        Assert.Equal(item.Title, Assert.Single(fixture.Editor.Items).Title);
    }

    [Fact]
    public void Delete_command_undo_restores_the_exact_entry()
    {
        using var fixture = new BehavioralFixture();
        var item = fixture.AddItem();
        item.Edit("Changed", MediaKind.Book, "Novel", ["long"], "Keep this.", "2/3");
        fixture.Editor.Update(item, "prepared");
        var command = new DeleteMediaCommand(fixture.Editor, item);

        fixture.Invoker.Execute(command);
        Assert.Empty(fixture.Editor.Items);

        fixture.Invoker.Undo();
        var restored = Assert.Single(fixture.Editor.Items);
        Assert.Equal(item.Id, restored.Id);
        Assert.Equal("Changed", restored.Title);
        Assert.Equal("Keep this.", restored.Notes);
        Assert.Equal("2/3", restored.Progress);
    }

    [Fact]
    public void Edit_command_undo_restores_previous_values()
    {
        using var fixture = new BehavioralFixture();
        var item = fixture.AddItem();
        var command = new EditMediaCommand(
            fixture.Editor,
            item,
            new MediaItemEdit("New title", MediaKind.Book, "New category", ["new"], "New notes", "5/5"));

        fixture.Invoker.Execute(command);
        Assert.Equal("New title", item.Title);
        Assert.Equal("New category", item.Category);

        fixture.Invoker.Undo();
        Assert.Equal("The Matrix", item.Title);
        Assert.Equal("Sci-Fi", item.Category);
        Assert.Equal("Notes.", item.Notes);
    }

    [Fact]
    public void Consume_undo_returns_to_planned()
    {
        using var fixture = new BehavioralFixture();
        var item = fixture.AddItem();
        var command = new ConsumeMediaCommand(fixture.Editor, item);

        fixture.Invoker.Execute(command);
        Assert.Equal(MediaStatus.Consumed, item.Status);

        fixture.Invoker.Undo();
        Assert.Equal(MediaStatus.Planned, item.Status);
        Assert.Null(item.Rating);
    }

    [Fact]
    public void Rate_undo_returns_to_consumed_without_rating()
    {
        using var fixture = new BehavioralFixture();
        var item = fixture.AddItem();
        item.MarkConsumed();
        fixture.Editor.Update(item, "consumed");
        var command = new RateMediaCommand(fixture.Editor, item, 9);

        fixture.Invoker.Execute(command);
        Assert.Equal(MediaStatus.Rated, item.Status);
        Assert.Equal(9, item.Rating);

        fixture.Invoker.Undo();
        Assert.Equal(MediaStatus.Consumed, item.Status);
        Assert.Null(item.Rating);
    }

    [Fact]
    public void New_command_clears_the_redo_stack()
    {
        using var fixture = new BehavioralFixture();
        var first = fixture.NewItem("First");
        fixture.Invoker.Execute(new AddMediaCommand(fixture.Editor, first));
        fixture.Invoker.Undo();
        Assert.True(fixture.Invoker.CanRedo);

        var second = fixture.NewItem("Second");
        fixture.Invoker.Execute(new AddMediaCommand(fixture.Editor, second));

        Assert.False(fixture.Invoker.CanRedo);
        Assert.Single(fixture.Editor.Items);
    }

    [Fact]
    public void Import_command_undo_removes_every_imported_entry()
    {
        using var fixture = new BehavioralFixture();
        var items = new[]
        {
            fixture.NewItem("Anime One", MediaKind.Anime),
            new MediaItemBuilder().WithTitle("Manga Two").WithKind(MediaKind.Manga).Build(fixture.Editor.NextId + 1),
        };
        var command = new ImportMediaCommand(fixture.Editor, items);

        fixture.Invoker.Execute(command);
        Assert.Equal(2, fixture.Editor.Items.Count);

        fixture.Invoker.Undo();
        Assert.Empty(fixture.Editor.Items);
    }

    [Fact]
    public void Lifecycle_command_rejects_rating_a_planned_entry()
    {
        using var fixture = new BehavioralFixture();
        var item = fixture.AddItem();

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Invoker.Execute(new RateMediaCommand(fixture.Editor, item, 8)));
        Assert.Equal(MediaStatus.Planned, item.Status);
        Assert.False(fixture.Invoker.CanUndo);
    }

    [Fact]
    public void Lifecycle_state_is_visible_through_the_context()
    {
        var item = new MediaItemBuilder().WithTitle("State").Build(1);
        var lifecycle = new MediaLifecycle(item);

        Assert.Equal("Planned", lifecycle.StateName);
        lifecycle.Consume();
        Assert.Equal("Consumed", lifecycle.StateName);
        lifecycle.Rate(7);
        Assert.Equal("Rated", lifecycle.StateName);
        lifecycle.Reopen();
        Assert.Equal("Planned", lifecycle.StateName);
        Assert.Null(item.Rating);
    }
}
