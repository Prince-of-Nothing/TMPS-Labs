using Lab1.Domain.Models;
using Lab1.Domain.Services;
using Lab1.Factory.FactoryMethod;
using Lab1.Factory.Singleton;

namespace Lab1;

public partial class MainPage : ContentPage
{
    private readonly WatchlistService _watchlist;

    public MainPage()
    {
        InitializeComponent();

        // Factory Method decides which storage to create.
        MediaLibraryFactory libraryFactory = new JsonLibraryFactory();

        // Singleton guarantees the whole app shares the same library.
        var library = MediaLibraryStore.Instance.EnsureInitialized(
            libraryFactory.CreateLibrary());

        _watchlist = new WatchlistService(library, new Domain.Scoring.RatingScoreCalculator());

        StatusLabel.Text = library.Description;
        Refresh();
    }

    private void OnAddClicked(object sender, EventArgs e)
    {
        try
        {
            _watchlist.AddMedia(TitleEntry.Text, CategoryEntry.Text, TagsEntry.Text);
            TitleEntry.Text = string.Empty;
            CategoryEntry.Text = string.Empty;
            TagsEntry.Text = string.Empty;
            StatusLabel.Text = "Added.";
            Refresh();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    private void Refresh()
    {
        ItemsList.ItemsSource = _watchlist.Items
            .Select(item => new ItemRow(item.Title, Describe(item)))
            .ToList();
    }

    private string Describe(MediaItem item)
    {
        var tags = item.Tags.Count == 0
            ? "no tags"
            : string.Join(", ", item.Tags.Select(t => t.Name));

        var score = item.Rating is int rating
            ? $"{rating}/10 (score {_watchlist.GetScore(item)}/100)"
            : item.IsConsumed ? "consumed, not rated" : "planned";

        return $"{item.Category.Name} · {tags} · {score}";
    }

    private sealed record ItemRow(string Title, string Detail);
}
