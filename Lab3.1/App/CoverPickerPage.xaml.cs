using Watchlist.Core.CoverSearch;
using Watchlist.Core.CoverSearch.Images;
using Watchlist.Core.Domain;

namespace Lab3;

public sealed record CoverRow(CoverCandidate Candidate, string Title, string Provider, ImageSource? Thumbnail);

public partial class CoverPickerPage : ContentPage
{
    private readonly AppServices _services;
    private readonly MediaItem _item;

    public CoverPickerPage(AppServices services, MediaItem item)
    {
        InitializeComponent();
        _services = services;
        _item = item;
        Title = $"Cover for {item.Title}";
        SearchEntry.Text = SuggestedQuery(item);
    }

    public event Action<CoverChoice>? Selected;

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        var query = SearchEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            StatusLabel.Text = "Type a title to search.";
            return;
        }

        try
        {
            StatusLabel.Text = "Searching...";
            var hits = await _services.Covers.SearchAsync(query, _item.Kind);
            ResultsList.ItemsSource = hits.Select(ToRow).ToArray();
            StatusLabel.Text = hits.Count == 0
                ? "No covers found. Try another query."
                : $"{hits.Count} results. Pick one to store it locally.";
        }
        catch (Exception ex) when (ex is HttpRequestException or CoverSearchException or TaskCanceledException)
        {
            StatusLabel.Text = $"Cover search failed: {ex.Message}";
        }
    }

    private async void OnUseClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CoverCandidate candidate })
        {
            return;
        }

        try
        {
            StatusLabel.Text = "Downloading...";
            var download = await _services.Covers.SaveAsync(_item.Id, candidate);
            StatusLabel.Text = $"Saved {download.RelativePath}.";
            Selected?.Invoke(new CoverChoice(download.RelativePath, candidate.ImageUrl));
            await Navigation.PopAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or CoverImageException or IOException)
        {
            StatusLabel.Text = $"Could not save that cover: {ex.Message}";
        }
    }

    private static CoverRow ToRow(CoverCandidate candidate) =>
        new(candidate, candidate.Title, candidate.Provider, Thumbnail(candidate.ThumbnailUrl));

    private static ImageSource? Thumbnail(string url)
    {
        try
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                ? ImageSource.FromUri(uri)
                : null;
        }
        catch (Exception ex) when (ex is ArgumentException or UriFormatException)
        {
            return null;
        }
    }

    private static string SuggestedQuery(MediaItem item) => item.Kind switch
    {
        MediaKind.Film or MediaKind.Series or MediaKind.Anime => $"{item.Title} poster",
        MediaKind.Book or MediaKind.Manga => $"{item.Title} cover",
        _ => item.Title,
    };
}
