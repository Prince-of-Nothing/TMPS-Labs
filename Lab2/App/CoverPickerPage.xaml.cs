using Watchlist.Core.CoverSearch;
using Watchlist.Core.CoverSearch.Images;
using Watchlist.Core.Domain;

namespace Lab2;

/// <summary>
/// Search goes through the decorated, adapted catalog; "Use" downloads
/// through the image proxy and stores the cover next to the Markdown entry.
/// </summary>
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
        RouteLabel.Text = $"{item.Kind} search: Filtering → Caching → Routing → provider Adapter";
    }

    /// <summary>Raised with a status message once a cover is stored.</summary>
    public event Action<string>? Saved;

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        var query = SearchEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            StatusLabel.Text = "Type a title to search.";
            return;
        }

        try
        {
            StatusLabel.Text = "Searching through the decorated catalog...";
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
            StatusLabel.Text = "Downloading through the image proxy...";
            var download = await _services.Covers.SaveAsync(_item.Id, candidate);
            _item.SetCover(download.RelativePath, candidate.ImageUrl);
            _services.Repository.Update(_item);
            Saved?.Invoke($"Saved {download.RelativePath} ({download.Bytes / 1024.0:F1} KB) from {candidate.Provider}.");
            await Navigation.PopAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or CoverImageException or IOException or TaskCanceledException)
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
        _ => item.Title,
    };
}
