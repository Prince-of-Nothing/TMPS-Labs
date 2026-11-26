using System.Text;
using Microsoft.Maui.Storage;
using Watchlist.Behavioral.Commands;
using Watchlist.Behavioral.Import;

namespace Lab3;

public partial class ImportPage : ContentPage
{
    private readonly AppServices _services;
    private ImportPreview? _preview;
    private string? _fileText;

    public ImportPage(AppServices services)
    {
        InitializeComponent();
        _services = services;
        SourcePicker.ItemsSource = _services.Importers.Select(importer => importer.Name).ToList();
        SourcePicker.SelectedIndex = 0;
        UpdateSourcePanels();
        StatusLabel.Text = "Choose a source and preview before importing.";
    }

    public event Action<int>? Imported;

    private void OnSourceChanged(object sender, EventArgs e)
    {
        _preview = null;
        _fileText = null;
        FileLabel.Text = "No file chosen.";
        ImportButton.IsEnabled = false;
        PreviewLabel.Text = "";
        ImportProgressBar.IsVisible = false;
        ProgressLabel.Text = "";
        UpdateSourcePanels();
    }

    private void UpdateSourcePanels()
    {
        var source = SourcePicker.SelectedItem as string ?? "AniList";
        var aniList = source == "AniList";
        AniListPanel.IsVisible = aniList;
        FilePanel.IsVisible = !aniList;
        FilePromptLabel.Text = source == "MAL XML" ? "MAL export file" : "CSV export file";
        ChooseFileButton.Text = source == "MAL XML" ? "Choose XML file" : "Choose CSV file";
        StatusLabel.Text = aniList
            ? "Enter a public AniList username."
            : source == "MAL XML"
                ? "Choose the XML from Profile → Export/Import in MAL."
                : "Choose a CSV with at least a Title column.";
    }

    private async void OnChooseFileClicked(object sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync();
            if (file is null)
            {
                return;
            }

            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            _fileText = await reader.ReadToEndAsync();
            FileLabel.Text = $"{file.FileName} ({_fileText.Length:N0} chars)";
            _preview = null;
            ImportButton.IsEnabled = false;
            PreviewLabel.Text = "";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusLabel.Text = $"Could not read that file: {ex.Message}";
        }
    }

    private async void OnPreviewClicked(object sender, EventArgs e)
    {
        try
        {
            StatusLabel.Text = "Reading the external list...";
            var sourceName = SourcePicker.SelectedItem as string ?? "AniList";
            var importer = _services.Importers.First(importer => importer.Name == sourceName);
            var input = sourceName == "AniList"
                ? UsernameEntry.Text?.Trim() ?? ""
                : _fileText ?? "";
            if (input.Length == 0)
            {
                StatusLabel.Text = sourceName == "AniList"
                    ? "Enter a username."
                    : "Choose a file.";
                return;
            }

            var rows = await importer.ImportAsync(input);
            _preview = _services.Import.Preview(rows, _services.Editor.Items);
            ImportButton.IsEnabled = _preview.NewItems.Count > 0;
            PreviewLabel.Text =
                $"{_preview.NewItems.Count} new · {_preview.Duplicates.Count} duplicate(s)\n" +
                string.Join("\n", _preview.NewItems.Take(8).Select(row => $"• {row.Title} ({row.Kind})")) +
                (_preview.NewItems.Count > 8 ? $"\n…and {_preview.NewItems.Count - 8} more" : "");
            StatusLabel.Text = "Preview ready. Import adds the new entries as one undoable command.";
        }
        catch (Exception ex) when (ex is ImportException or HttpRequestException or TaskCanceledException)
        {
            StatusLabel.Text = $"Import preview failed: {ex.Message}";
            ImportButton.IsEnabled = false;
        }
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        if (_preview is null || _preview.NewItems.Count == 0)
        {
            return;
        }

        var nextId = _services.Editor.NextId;
        var items = _services.Import.BuildItems(_preview, () => nextId++);
        ImportButton.IsEnabled = false;
        PreviewButton.IsEnabled = false;
        ImportProgressBar.IsVisible = true;
        ImportProgressBar.Progress = 0;
        ProgressLabel.Text = $"Applying 0/{items.Count}...";

        var progress = new Progress<ImportProgress>(item =>
        {
            ImportProgressBar.Progress = item.Total == 0 ? 0 : (double)item.Processed / item.Total;
            ProgressLabel.Text = $"Applying {item.Processed}/{item.Total}: {item.Title}";
        });

        try
        {
            await Task.Run(() =>
                _services.Commands.Execute(new ImportMediaCommand(_services.Editor, items, progress)));
            ImportProgressBar.Progress = 1;
            ProgressLabel.Text = $"Applied {items.Count} entries.";
            StatusLabel.Text =
                $"Imported {items.Count} entries; skipped {_preview.Duplicates.Count} duplicate(s).";
            Imported?.Invoke(items.Count);
            await DisplayAlertAsync(
                "Import complete",
                $"{items.Count} entries were added as one command. Undo can remove them.",
                "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException)
        {
            StatusLabel.Text = $"Import failed and was rolled back: {ex.Message}";
            ProgressLabel.Text = "No entries were kept.";
            ImportButton.IsEnabled = true;
            PreviewButton.IsEnabled = true;
        }
    }
}
