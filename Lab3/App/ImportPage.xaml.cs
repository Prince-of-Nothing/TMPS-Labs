using System.Text;
using Microsoft.Maui.Storage;
using Watchlist.Behavioral.Commands;
using Watchlist.Behavioral.Import;

namespace Lab3;

public partial class ImportPage : ContentPage
{
    private readonly AppServices _services;
    private ImportPreview? _preview;
    private string? _malXml;

    public ImportPage(AppServices services)
    {
        InitializeComponent();
        _services = services;
        SourcePicker.ItemsSource = _services.Importers.Select(importer => importer.Name).ToList();
        SourcePicker.SelectedIndex = 0;
        StatusLabel.Text = "Choose a source and preview before importing.";
    }

    public event Action<int>? Imported;

    private void OnSourceChanged(object sender, EventArgs e)
    {
        var aniList = SourcePicker.SelectedItem as string == "AniList";
        AniListPanel.IsVisible = aniList;
        MalPanel.IsVisible = !aniList;
        _preview = null;
        ImportButton.IsEnabled = false;
        PreviewLabel.Text = "";
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
            _malXml = await reader.ReadToEndAsync();
            FileLabel.Text = $"{file.FileName} ({_malXml.Length:N0} chars)";
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
            var sourceName = SourcePicker.SelectedItem as string;
            var importer = _services.Importers.First(importer => importer.Name == sourceName);
            var input = sourceName == "AniList"
                ? UsernameEntry.Text?.Trim() ?? ""
                : _malXml ?? "";
            if (input.Length == 0)
            {
                StatusLabel.Text = sourceName == "AniList"
                    ? "Enter a username."
                    : "Choose a MAL XML file.";
                return;
            }

            var rows = await importer.ImportAsync(input);
            _preview = _services.Import.Preview(rows, _services.Editor.Items);
            ImportButton.IsEnabled = _preview.NewItems.Count > 0;
            PreviewLabel.Text =
                $"{_preview.NewItems.Count} new · {_preview.Duplicates.Count} duplicate(s)\n" +
                string.Join("\n", _preview.NewItems.Take(8).Select(row => $"• {row.Title} ({row.Kind})")) +
                (_preview.NewItems.Count > 8 ? $"\n…and {_preview.NewItems.Count - 8} more" : "");
            StatusLabel.Text = "Preview ready. Import will add the new entries as one undoable command.";
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

        try
        {
            var nextId = _services.Editor.NextId;
            var items = _services.Import.BuildItems(_preview, () => nextId++);
            _services.Commands.Execute(new ImportMediaCommand(_services.Editor, items));
            Imported?.Invoke(items.Count);
            await DisplayAlertAsync("Imported", $"{items.Count} entries were added. Undo can remove them.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException)
        {
            StatusLabel.Text = $"Import failed: {ex.Message}";
        }
    }
}
