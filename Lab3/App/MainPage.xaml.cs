using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using Watchlist.Behavioral.Commands;
using Watchlist.Behavioral.Export;
using Watchlist.Behavioral.Observers;
using Watchlist.Core.Domain;

namespace Lab3;

public partial class MainPage : ContentPage
{
    private readonly AppServices _services;
    private readonly StatisticsObserver _statistics;
    private readonly ActivityLogObserver _activity = new();
    private readonly DelegateObserver _refreshObserver;
    private bool _liveStats;

    public MainPage(AppServices services)
    {
        InitializeComponent();
        _services = services;
        _statistics = new StatisticsObserver(() => _services.Editor.Items);
        _refreshObserver = new DelegateObserver(change =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = change.Message;
                RefreshRows();
            });
        });

        _services.Events.Subscribe(_refreshObserver);
        _services.Events.Subscribe(_activity);
        _services.Commands.HistoryChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(UpdateHistory);
        _statistics.Updated += (_, _) =>
        {
            if (_liveStats)
            {
                MainThread.BeginInvokeOnMainThread(UpdateStatistics);
            }
        };

        StatusFilter.ItemsSource = new List<string> { "All", "Planned", "Completed", "Consumed", "Rated" };
        StatusFilter.SelectedIndex = 0;
        SortPicker.ItemsSource = new List<string> { "Title", "Rating", "Recently updated", "Kind" };
        SortPicker.SelectedIndex = 0;

        SubtitleLabel.Text = $"Markdown vault · {_services.RootPath}";
        StatusLabel.Text = "Command + Observer + State + Strategy";
        RefreshRows();
        UpdateHistory();
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var page = new EditPage(_services, null);
        page.Saved += draft =>
        {
            try
            {
                var item = BuildItem(draft);
                _services.Commands.Execute(new AddMediaCommand(_services.Editor, item));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                StatusLabel.Text = ex.Message;
            }
        };
        await Navigation.PushAsync(page);
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (FindItem(sender) is not { } item)
        {
            return;
        }

        var page = new EditPage(_services, item);
        page.Saved += draft =>
        {
            try
            {
                var edit = new MediaItemEdit(
                    draft.Title,
                    draft.Kind,
                    draft.Category,
                    SplitTags(draft.Tags),
                    draft.Notes,
                    draft.Progress,
                    draft.CoverPath,
                    draft.CoverSourceUrl,
                    draft.CustomKind);
                _services.Commands.Execute(new EditMediaCommand(_services.Editor, item, edit));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                StatusLabel.Text = ex.Message;
            }
        };
        await Navigation.PushAsync(page);
    }

    private void OnConsumeClicked(object sender, EventArgs e)
    {
        if (FindItem(sender) is { } item)
        {
            Run(() => _services.Commands.Execute(new ConsumeMediaCommand(_services.Editor, item)));
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

        Run(() => _services.Commands.Execute(new RateMediaCommand(_services.Editor, item, rating)));
    }

    private void OnReopenClicked(object sender, EventArgs e)
    {
        if (FindItem(sender) is { } item)
        {
            Run(() => _services.Commands.Execute(new ReopenMediaCommand(_services.Editor, item)));
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
            $"Delete “{item.Title}” and its local cover?",
            "Delete",
            "Cancel");
        if (confirmed)
        {
            Run(() => _services.Commands.Execute(new DeleteMediaCommand(_services.Editor, item)));
        }
    }

    private async void OnCoverClicked(object sender, EventArgs e)
    {
        if (FindItem(sender) is not { } item)
        {
            return;
        }

        var page = new CoverPickerPage(_services, item);
        page.Selected += choice =>
        {
            Run(() => _services.Commands.Execute(new SetCoverCommand(
                _services.Editor,
                item,
                choice.RelativePath,
                choice.SourceUrl)));
        };
        await Navigation.PushAsync(page);
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        var page = new ImportPage(_services);
        page.Imported += count => StatusLabel.Text = $"Imported {count} entries from the external list.";
        await Navigation.PushAsync(page);
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        var choice = await DisplayActionSheetAsync("Export as", "Cancel", null, "CSV", "Markdown");
        if (choice is not ("CSV" or "Markdown"))
        {
            return;
        }

        try
        {
            _services.Exports.Use(choice == "CSV"
                ? new CsvWatchlistExporter()
                : new MarkdownWatchlistExporter());
            var content = _services.Exports.Export(_services.Editor.Items);
            var path = Path.Combine(
                FileSystem.CacheDirectory,
                $"watchlist-{DateTime.Now:yyyyMMdd-HHmm}{_services.Exports.FileExtension}");
            await File.WriteAllTextAsync(path, content);
            StatusLabel.Text = $"Exported {_services.Exports.Name} to {path}";
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Watchlist export",
                File = new ShareFile(path),
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusLabel.Text = $"Export failed: {ex.Message}";
        }
    }

    private void OnUndoClicked(object sender, EventArgs e)
    {
        Run(_services.Commands.Undo);
        UpdateHistory();
    }

    private void OnRedoClicked(object sender, EventArgs e)
    {
        Run(_services.Commands.Redo);
        UpdateHistory();
    }

    private void OnReloadClicked(object sender, EventArgs e)
    {
        Run(() =>
        {
            _services.Editor.Reload();
            _services.Commands.Clear();
            if (_services.Editor.Errors.Count > 0)
            {
                StatusLabel.Text = $"{_services.Editor.Errors.Count} unreadable file(s); see the vault.";
            }
        });
        RefreshRows();
    }

    private async void OnSettingsClicked(object sender, EventArgs e) =>
        await Navigation.PushAsync(new SettingsPage(_services));

    private async void OnActivityClicked(object sender, EventArgs e)
    {
        var entries = _activity.Entries.TakeLast(12).ToArray();
        await DisplayAlertAsync(
            "Activity",
            entries.Length == 0 ? "No activity yet." : string.Join("\n", entries),
            "OK");
    }

    private void OnLiveStatsToggled(object sender, ToggledEventArgs e)
    {
        _liveStats = e.Value;
        if (_liveStats)
        {
            _services.Events.Subscribe(_statistics);
        }
        else
        {
            _services.Events.Unsubscribe(_statistics);
        }

        UpdateStatistics();
    }

    private void OnFilterChanged(object sender, EventArgs e) => RefreshRows();

    private void OnSearchChanged(object sender, TextChangedEventArgs e) => RefreshRows();

    private async void OnRefreshing(object sender, EventArgs e)
    {
        OnReloadClicked(sender, e);
        await Task.Delay(250);
        Refresh.IsRefreshing = false;
    }

    private void RefreshRows()
    {
        var filter = StatusFilter.SelectedItem as string ?? "All";
        var sort = SortPicker.SelectedItem as string ?? "Title";
        var search = SearchEntry.Text?.Trim();
        IEnumerable<MediaItem> query = _services.Editor.Items;

        if (filter == "Completed")
        {
            query = query.Where(item => item.Status is MediaStatus.Consumed or MediaStatus.Rated);
        }
        else if (filter is "Planned" or "Consumed" or "Rated")
        {
            query = query.Where(item => item.Status.ToString() == filter);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item =>
                item.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                item.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                item.Tags.Any(tag => tag.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        query = sort switch
        {
            "Rating" => query.OrderByDescending(item => item.Rating ?? 0).ThenBy(item => item.Title),
            "Recently updated" => query.OrderByDescending(item => item.UpdatedAt),
            "Kind" => query.OrderBy(item => item.Kind).ThenBy(item => item.Title),
            _ => query.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase),
        };

        ItemsList.ItemsSource = query.Select(item => ToRow(item)).ToArray();
        UpdateStatistics();
        UpdateHistory();
    }

    private ItemRow ToRow(MediaItem item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Subtitle = $"{item.KindLabel} · {item.Category}" +
                   (item.Tags.Count == 0 ? "" : $" · {string.Join(", ", item.Tags)}"),
        StatusText = item.Status switch
        {
            MediaStatus.Rated => $"Rated {item.Rating}/10",
            MediaStatus.Consumed => "Consumed",
            _ => "Planned",
        },
        StatusColor = item.Status switch
        {
            MediaStatus.Rated => "#F3DDE6",
            MediaStatus.Consumed => "#DDEBDD",
            _ => "#E8E1F0",
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

    private MediaItem BuildItem(EntryDraft draft) =>
        new MediaItemBuilder()
            .WithTitle(draft.Title)
            .WithKind(draft.Kind)
            .WithCustomKind(draft.CustomKind)
            .WithCategory(draft.Category)
            .WithTags(SplitTags(draft.Tags))
            .WithNotes(draft.Notes)
            .WithProgress(draft.Progress)
            .WithCover(draft.CoverPath, draft.CoverSourceUrl)
            .Build(_services.Editor.NextId);

    private MediaItem? FindItem(object sender) =>
        sender is Button { CommandParameter: int id } ? _services.Editor.Find(id) : null;

    private void UpdateStatistics()
    {
        var stats = _liveStats ? _statistics.Statistics : ComputeStatistics();
        TotalValue.Text = stats.Total.ToString();
        PlannedValue.Text = stats.Planned.ToString();
        ConsumedValue.Text = stats.Consumed.ToString();
        RatedValue.Text = stats.Rated.ToString();
        AverageLabel.Text = stats.AverageRating > 0 ? $"avg {stats.AverageRating:F1}/10" : "";
    }

    private WatchlistStatistics ComputeStatistics()
    {
        var items = _services.Editor.Items;
        var ratings = items.Where(item => item.Rating is not null).Select(item => item.Rating!.Value).ToArray();
        return new WatchlistStatistics(
            items.Count,
            items.Count(item => item.Status == MediaStatus.Planned),
            items.Count(item => item.Status == MediaStatus.Consumed),
            items.Count(item => item.Status == MediaStatus.Rated),
            ratings.Length == 0 ? 0 : ratings.Average());
    }

    private void UpdateHistory()
    {
        UndoButton.IsEnabled = _services.Commands.CanUndo;
        RedoButton.IsEnabled = _services.Commands.CanRedo;
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
    }

    private static IReadOnlyList<string> SplitTags(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
