using Watchlist.Core.Domain;

namespace Lab3;

public partial class EditPage : ContentPage
{
    private readonly AppServices _services;
    private readonly MediaItem? _item;
    private string? _coverPath;
    private string? _coverSource;

    public EditPage(AppServices services, MediaItem? item)
    {
        InitializeComponent();
        _services = services;
        _item = item;

        KindPicker.ItemsSource = Enum.GetValues<MediaKind>().Select(value => value.ToString()).ToList();

        if (item is null)
        {
            HeaderLabel.Text = "New entry";
            KindPicker.SelectedItem = MediaKind.Other.ToString();
            ChooseCoverButton.IsVisible = false;
            CoverLabel.Text = "Add the entry first, then use Cover on its card.";
            return;
        }

        HeaderLabel.Text = "Edit entry";
        TitleEntry.Text = item.Title;
        KindPicker.SelectedItem = item.Kind.ToString();
        CategoryEntry.Text = item.Category;
        TagsEntry.Text = string.Join(", ", item.Tags);
        ProgressEntry.Text = item.Progress ?? "";
        NotesEditor.Text = item.Notes;
        _coverPath = item.CoverPath;
        _coverSource = item.CoverSourceUrl;
        UpdateCoverPreview();
    }

    public event Action<EntryDraft>? Saved;

    private async void OnChooseCoverClicked(object sender, EventArgs e)
    {
        if (_item is null)
        {
            return;
        }

        var page = new CoverPickerPage(_services, _item);
        page.Selected += choice =>
        {
            _coverPath = choice.RelativePath;
            _coverSource = choice.SourceUrl;
            UpdateCoverPreview();
        };
        await Navigation.PushAsync(page);
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var title = TitleEntry.Text?.Trim() ?? "";
        if (title.Length == 0)
        {
            StatusLabel.Text = "A title is required.";
            return;
        }

        var kind = Enum.TryParse<MediaKind>(KindPicker.SelectedItem as string, out var parsed)
            ? parsed
            : MediaKind.Other;
        var draft = new EntryDraft(
            title,
            kind,
            CategoryEntry.Text?.Trim() ?? "",
            TagsEntry.Text?.Trim() ?? "",
            NotesEditor.Text?.Trim() ?? "",
            string.IsNullOrWhiteSpace(ProgressEntry.Text) ? null : ProgressEntry.Text.Trim(),
            _coverPath,
            _coverSource);

        Saved?.Invoke(draft);
        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e) => await Navigation.PopAsync();

    private void UpdateCoverPreview()
    {
        CoverPreview.Source = null;
        if (_coverPath is not null)
        {
            var absolute = Path.Combine(
                _services.RootPath,
                _coverPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absolute))
            {
                CoverPreview.Source = ImageSource.FromFile(absolute);
                CoverLabel.Text = _coverPath;
                return;
            }
        }

        if (_coverSource is not null &&
            Uri.TryCreate(_coverSource, UriKind.Absolute, out var uri))
        {
            CoverPreview.Source = ImageSource.FromUri(uri);
            CoverLabel.Text = "Remote cover; choose one to store it locally.";
            return;
        }

        CoverLabel.Text = "No local cover yet.";
    }
}
