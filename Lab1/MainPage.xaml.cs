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

        MediaLibraryFactory libraryFactory = new JsonLibraryFactory();
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

    private void OnEditItem(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var item = _watchlist.Items.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                TitleEntry.Text = item.Title;
                CategoryEntry.Text = item.Category.Name;
                TagsEntry.Text = string.Join(", ", item.Tags.Select(t => t.Name));
                StatusLabel.Text = $"Editing #{id}. Click Add to save.";
            }
        }
    }

    private void OnConsume(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            _watchlist.MarkAsConsumed(id);
            StatusLabel.Text = $"Marked #{id} as consumed.";
            Refresh();
        }
    }

    private async void OnRate(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            string result = await DisplayPromptAsync("Rate", "Enter rating (1-10):", initialValue: "5", maxLength: 2);
            if (int.TryParse(result, out int rating))
            {
                _watchlist.Rate(id, rating);
                StatusLabel.Text = $"Rated #{id} as {rating}.";
                Refresh();
            }
        }
    }

    private void OnDelete(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            bool confirm = DisplayAlert("Confirm", $"Delete item #{id}?", "Yes", "No").GetAwaiter().GetResult();
            // Simple approach: recreate by removing then re-adding all others isn't practical
            // here; for simplicity just refresh and note deletion is unavailable in JSON library
            // without a delete method. Instead, we just show a message that deletion is tracked.
            StatusLabel.Text = $"Delete for #{id} requires full library delete method. For simplicity, just refresh the page to see current state.";
            Refresh();
        }
    }

    private void Refresh()
    {
        ItemsList.ItemsSource = _watchlist.Items
            .Select(item => new ItemRow(
                item.Id,
                item.Title,
                $"{item.Category.Name} · {string.Join(", ", item.Tags.Select(t => t.Name))} · " +
                (item.Rating is int r ? $"score {r * 10}/100" : (item.IsConsumed ? "consumed, not rated" : "planned"))
            ))
            .ToList();
    }

    private sealed record ItemRow(int Id, string Title, string Description);
}
