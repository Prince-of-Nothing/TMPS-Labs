using Watchlist.Core.Domain;

namespace Lab2;

/// <summary>
/// The Lab 2 client. It edits entries through <c>WatchlistRepository</c> and
/// reaches the structural cover pipeline only through <c>CoverService</c>.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly AppServices _services;

    public MainPage(AppServices services)
    {
        InitializeComponent();
        _services = services;
        SubtitleLabel.Text = $"Markdown vault · {_services.RootPath}";
        StatusLabel.Text = "Adapter + Decorator + Proxy on the cover workflow";
        RefreshRows();
        ReportVaultErrors();
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var page = new EditPage(null);
        page.Saved += draft => Run(() =>
        {
            var item = new MediaItemBuilder()
                .WithTitle(draft.Title)
                .WithKind(draft.Kind)
                .WithCategory(draft.Category)
                .WithTags(SplitTags(draft.Tags))
                .WithNotes(draft.Notes)
                .WithProgress(draft.Progress)
                .Build(_services.Repository.NextId);
            _services.Repository.Add(item);
            StatusLabel.Text = $"Added “{item.Title}”.";
        });
        await Navigation.PushAsync(page);
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (FindItem(sender) is not { } item)
        {
            return;
        }

        var page = new EditPage(item);
        page.Saved += draft => Run(() =>
        {
            item.Edit(
                draft.Title,
                draft.Kind,
                draft.Category,
                SplitTags(draft.Tags),
                draft.Notes,
                draft.Progress);
            _services.Repository.Update(item);
            StatusLabel.Text = $"Updated “{item.Title}”.";
        });
        await Navigation.PushAsync(page);
    }

    private void OnConsumeClicked(object sender, EventArgs e)
    {
        if (FindItem(sender) is { } item)
        {
            Run(() =>
            {
                item.MarkConsumed();
                _services.Repository.Update(item);
                StatusLabel.Text = $"“{item.Title}” is consumed.";
            });
        }
    }

    private async void OnRateClicked(object sender, EventArgs e)
    {
        if (FindItem(sender) is not { } item)
        {
            return;
        }

        var raw = await DisplayPromptAsync(
            "Rate",
            $"Rate “{item.Title}” from 1 to 10:",
            initialValue: item.Rating?.ToString() ?? "8",
            maxLength: 2,
            keyboard: Keyboard.Numeric);
        if (!int.TryParse(raw, out var rating))
        {
            return;
        }

        Run(() =>
        {
            item.Rate(rating);
            _services.Repository.Update(item);
            StatusLabel.Text = $"Rated “{item.Title}” {rating}/10.";
        });
    }

    private void OnReopenClicked(object sender, EventArgs e)
    {
        if (FindItem(sender) is { } item)
        {
            Run(() =>
            {
                item.Reopen();
                _services.Repository.Update(item);
                StatusLabel.Text = $"“{item.Title}” is planned again.";
            });
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (FindItem(sender) is not { } item)
        {
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Delete entry",
            $"Delete “{item.Title}” and its local cover? This cannot be undone.",
            "Delete",
            "Cancel");
        if (confirmed)
        {
            Run(() =>
            {
                _services.Repository.Delete(item.Id);
                StatusLabel.Text = $"Deleted “{item.Title}”.";
            });
        }
    }

    private async void OnCoverClicked(object sender, EventArgs e)
    {
        if (FindItem(sender) is not { } item)
        {
            return;
        }

        var page = new CoverPickerPage(_services, item);
        page.Saved += message =>
        {
            StatusLabel.Text = message;
            RefreshRows();
        };
        await Navigation.PushAsync(page);
    }

    private async void OnPatternsClicked(object sender, EventArgs e) =>
        await Navigation.PushAsync(new PatternsPage());

    private void OnReloadClicked(object sender, EventArgs e)
    {
        Run(() =>
        {
            _services.Repository.Reload();
            StatusLabel.Text = $"Reloaded {_services.Repository.Items.Count} entries from Markdown.";
        });
        ReportVaultErrors();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        OnReloadClicked(sender, e);
        await Task.Delay(250);
        Refresh.IsRefreshing = false;
    }

    private void RefreshRows()
    {
        var items = _services.Repository.Items;
        ItemsList.ItemsSource = items
            .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .Select(ToRow)
            .ToArray();

        TotalValue.Text = items.Count.ToString();
        PlannedValue.Text = items.Count(item => item.Status == MediaStatus.Planned).ToString();
        ConsumedValue.Text = items.Count(item => item.Status == MediaStatus.Consumed).ToString();
        RatedValue.Text = items.Count(item => item.Status == MediaStatus.Rated).ToString();
    }

    private ItemRow ToRow(MediaItem item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Subtitle = $"{item.Kind} · {item.Category}" +
                   (item.Tags.Count == 0 ? "" : $" · {string.Join(", ", item.Tags)}"),
        StatusText = item.Status switch
        {
            MediaStatus.Rated => $"Rated {item.Rating}/10",
            MediaStatus.Consumed => "Consumed",
            _ => "Planned",
        },
        StatusColor = item.Status switch
        {
            MediaStatus.Rated => "#F1E2CF",
            MediaStatus.Consumed => "#DDE9D2",
            _ => "#D9E9E7",
        },
        ProgressText = item.Progress is null ? "" : $"progress {item.Progress}",
        NotesPreview = item.Notes.Replace('\n', ' ').Replace('\r', ' '),
        HasNotes = !string.IsNullOrWhiteSpace(item.Notes),
        IsPlanned = item.Status == MediaStatus.Planned,
        CanRate = item.Status != MediaStatus.Planned,
        CanReopen = item.Status != MediaStatus.Planned,
        Cover = ResolveCover(item),
    };

    private ImageSource? ResolveCover(MediaItem item)
    {
        try
        {
            if (item.CoverPath is not null)
            {
                var absolute = Path.Combine(
                    _services.RootPath,
                    item.CoverPath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(absolute))
                {
                    return ImageSource.FromFile(absolute);
                }
            }

            if (item.CoverSourceUrl is not null &&
                Uri.TryCreate(item.CoverSourceUrl, UriKind.Absolute, out var uri))
            {
                return ImageSource.FromUri(uri);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or UriFormatException)
        {
            // A broken cover link must not hide the entry.
        }

        return null;
    }

    private MediaItem? FindItem(object sender) =>
        sender is Button { CommandParameter: int id } ? _services.Repository.Find(id) : null;

    private void ReportVaultErrors()
    {
        var errors = _services.Repository.Errors;
        if (errors.Count > 0)
        {
            StatusLabel.Text = $"{errors.Count} unreadable file(s) were skipped and left untouched.";
        }
    }

    private void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException)
        {
            StatusLabel.Text = ex.Message;
        }

        RefreshRows();
    }

    private static IReadOnlyList<string> SplitTags(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
